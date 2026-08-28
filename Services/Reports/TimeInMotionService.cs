using inventory_api.Data;
using inventory_api.DTOs.Reports.TimeInMotion;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Reports.TimeInMotion
{
    public class TimeInMotionService
    {
        private readonly AppDbContext _context;

        public TimeInMotionService(
            AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<TimeInMotionListDto>>
            GetAllAsync()
        {
            // ============================================================
            // LOAD MAIN RECORDS
            // ============================================================

            var incomingList =
                await _context.IncomingReceivings
                    .AsNoTracking()
                    .OrderByDescending(x =>
                        x.IncomingReceivingId)
                    .ToListAsync();


            var qcList =
                await _context.QcInspectionHeaders
                    .AsNoTracking()
                    .ToListAsync();


            var quarantineList =
                await _context.QuarantineHeaders
                    .AsNoTracking()
                    .ToListAsync();


            var processingList =
                await _context.RmwProcessingHeaders
                    .AsNoTracking()
                    .Include(x => x.Lines)
                    .ToListAsync();


            var finalReceivingList =
    await _context.ReceivingReportHeaders
        .AsNoTracking()
        .ToListAsync();


            var suppliers =
                await _context.Suppliers
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.SupplierId,
                        x => x.SupplierName
                    );


            var branches =
                await _context.Branches
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.branch_id,
                        x => x.branch_name
                    );


            var users =
                await _context.Users
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.user_id,
                        x => x.full_name
                    );


            var result =
                new List<TimeInMotionListDto>();


            // ============================================================
            // BUILD ONE REPORT PER INCOMING RECEIVING
            // ============================================================

            foreach (var incoming in incomingList)
            {
                var qc =
                    qcList
                        .Where(x =>
                            x.IncomingReceivingId ==
                            incoming.IncomingReceivingId)
                        .OrderByDescending(x =>
                            x.QcId)
                        .FirstOrDefault();


                var quarantine =
                    quarantineList
                        .Where(x =>
                            x.IncomingReceivingId ==
                            incoming.IncomingReceivingId)
                        .OrderByDescending(x =>
                            x.QuarantineId)
                        .FirstOrDefault();


                var processing =
                    processingList
                        .Where(x =>
                            x.IncomingReceivingId ==
                            incoming.IncomingReceivingId)
                        .OrderByDescending(x =>
                            x.ProcessingId)
                        .FirstOrDefault();


                var finalReceiving =
     finalReceivingList
         .Where(x =>
             x.ScheduleId ==
             incoming.ScheduleId)
         .OrderByDescending(x =>
             x.RrId)
         .FirstOrDefault();


                // ========================================================
                // RMW LOT TIMESTAMPS
                // ========================================================

                DateTime? weighingStartedAt =
                    processing?.Lines
                        .Where(x =>
                            x.WeighingStartedAt.HasValue)
                        .Select(x =>
                            x.WeighingStartedAt)
                        .OrderBy(x => x)
                        .FirstOrDefault();


                DateTime? weighingCompletedAt =
                    processing?.Lines
                        .Where(x =>
                            x.WeighingCompletedAt.HasValue)
                        .Select(x =>
                            x.WeighingCompletedAt)
                        .OrderByDescending(x => x)
                        .FirstOrDefault();


                DateTime? stickerCompletedAt =
                    processing?.Lines
                        .Where(x =>
                            x.StickerCompletedAt.HasValue)
                        .Select(x =>
                            x.StickerCompletedAt)
                        .OrderByDescending(x => x)
                        .FirstOrDefault();


                // ========================================================
                // USERS
                // ========================================================

                string GetUserName(
                    string? userId)
                {
                    if (string.IsNullOrWhiteSpace(
                        userId))
                    {
                        return "";
                    }

                    return users.TryGetValue(
                        userId,
                        out var name)
                            ? name
                            : userId;
                }


                // ========================================================
                // TIMELINE
                // ========================================================

                var stages =
                    new List<TimeInMotionStageDto>();


                // --------------------------------------------------------
                // 1. INCOMING RECEIVING
                // --------------------------------------------------------

                stages.Add(
                    new TimeInMotionStageDto
                    {
                        Stage =
                            "Incoming Receiving",

                        StartAt =
                            incoming.CreatedAt,

                        EndAt =
                            qc?.CreatedAt,

                        DurationMinutes =
                            GetDurationMinutes(
                                incoming.CreatedAt,
                                qc?.CreatedAt
                            ),

                        ResponsibleUserId =
                            incoming.CreatedBy,

                        ResponsibleUserName =
                            GetUserName(
                                incoming.CreatedBy),

                        Status =
                            incoming.ReceivingStatus
                    }
                );


                // --------------------------------------------------------
                // 2. QA/QC INSPECTION
                // --------------------------------------------------------

                if (qc != null)
                {
                    var qcEndAt =
                        qc.UpdatedAt
                        ?? qc.InspectionDate
                        ?? quarantine?.CreatedAt;


                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "QA/QC Inspection",

                            StartAt =
                                qc.CreatedAt,

                            EndAt =
                                qcEndAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    qc.CreatedAt,
                                    qcEndAt
                                ),

                            ResponsibleUserId =
                                qc.InspectorId,

                            ResponsibleUserName =
                                GetUserName(
                                    qc.InspectorId),

                            Status =
                                qc.Status
                        }
                    );
                }


                // --------------------------------------------------------
                // 3. QUARANTINE / HOLD
                // --------------------------------------------------------

                if (quarantine != null)
                {
                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "Quarantine / Hold",

                            StartAt =
                                quarantine.CreatedAt,

                            EndAt =
                                quarantine.ReleasedAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    quarantine.CreatedAt,
                                    quarantine.ReleasedAt
                                ),

                            ResponsibleUserId =
                                quarantine.ReleasedBy
                                ?? quarantine.CreatedBy,

                            ResponsibleUserName =
                                GetUserName(
                                    quarantine.ReleasedBy
                                    ?? quarantine.CreatedBy),

                            Status =
                                quarantine.Status
                        }
                    );
                }

                // --------------------------------------------------------
                // 4. RELEASED TO RMW
                // --------------------------------------------------------

                if (processing != null)
                {
                    // RMW waiting time starts immediately
                    // after Quarantine releases the material.
                    var releasedToRmwStartAt =
                        quarantine?.ReleasedAt
                        ?? processing.CreatedAt;

                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "Released to RMW",

                            StartAt =
                                releasedToRmwStartAt,

                            EndAt =
                                weighingStartedAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    releasedToRmwStartAt,
                                    weighingStartedAt
                                ),

                            ResponsibleUserId =
                                processing.CreatedBy,

                            ResponsibleUserName =
                                GetUserName(
                                    processing.CreatedBy),

                            Status =
                                weighingStartedAt.HasValue
                                    ? "COMPLETED"
                                    : processing.Status
                        }
                    );
                }


                // --------------------------------------------------------
                // 5. WEIGHING / COUNTING
                // --------------------------------------------------------

                if (
                    weighingStartedAt.HasValue ||
                    weighingCompletedAt.HasValue
                )
                {
                    var weighingUserId =
                        processing?.Lines
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.WeighingCompletedBy))
                            .OrderByDescending(x =>
                                x.WeighingCompletedAt)
                            .Select(x =>
                                x.WeighingCompletedBy)
                            .FirstOrDefault()

                        ?? processing?.Lines
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.WeighingStartedBy))
                            .OrderBy(x =>
                                x.WeighingStartedAt)
                            .Select(x =>
                                x.WeighingStartedBy)
                            .FirstOrDefault();


                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "Weighing / Counting",

                            StartAt =
                                weighingStartedAt,

                            EndAt =
                                weighingCompletedAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    weighingStartedAt,
                                    weighingCompletedAt
                                ),

                            ResponsibleUserId =
                                weighingUserId,

                            ResponsibleUserName =
                                GetUserName(
                                    weighingUserId),

                            Status =
                                weighingCompletedAt.HasValue
                                    ? "COMPLETED"
                                    : "IN_PROGRESS"
                        }
                    );
                }


                // --------------------------------------------------------
                // 6. STICKER / IDENTIFICATION
                // --------------------------------------------------------

                if (
                    weighingCompletedAt.HasValue ||
                    stickerCompletedAt.HasValue
                )
                {
                    var stickerUserId =
                        processing?.Lines
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(
                                    x.StickerCompletedBy))
                            .OrderByDescending(x =>
                                x.StickerCompletedAt)
                            .Select(x =>
                                x.StickerCompletedBy)
                            .FirstOrDefault();


                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "Sticker / Identification",

                            StartAt =
                                weighingCompletedAt,

                            EndAt =
                                stickerCompletedAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    weighingCompletedAt,
                                    stickerCompletedAt
                                ),

                            ResponsibleUserId =
                                stickerUserId,

                            ResponsibleUserName =
                                GetUserName(
                                    stickerUserId),

                            Status =
                                stickerCompletedAt.HasValue
                                    ? "COMPLETED"
                                    : "PENDING"
                        }
                    );
                }


                // --------------------------------------------------------
                // 7. FINAL RR / INVENTORY COMMIT
                // --------------------------------------------------------

                // --------------------------------------------------------
                // 7. FINAL RR / INVENTORY COMMIT
                // --------------------------------------------------------

                if (finalReceiving != null)
                {
                    // Final RR starts as soon as Sticker / Identification
                    // has been completed.
                    var finalRrStartAt =
                        stickerCompletedAt
                        ?? finalReceiving.CreatedAt;

                    stages.Add(
                        new TimeInMotionStageDto
                        {
                            Stage =
                                "Final RR / Inventory Commit",

                            StartAt =
                                finalRrStartAt,

                            EndAt =
                                finalReceiving.CommittedAt,

                            DurationMinutes =
                                GetDurationMinutes(
                                    finalRrStartAt,
                                    finalReceiving.CommittedAt
                                ),

                            ResponsibleUserId =
                                finalReceiving.CommittedBy
                                ?? finalReceiving.CreatedBy,

                            ResponsibleUserName =
                                GetUserName(
                                    finalReceiving.CommittedBy
                                    ?? finalReceiving.CreatedBy),

                            Status =
                                finalReceiving.Status
                        }
                    );
                }


                // ========================================================
                // OVERALL TIMES
                // ========================================================

                var startedAt =
                    incoming.CreatedAt;


                var completedAt =
                    finalReceiving?.CommittedAt;


                // ========================================================
                // CURRENT STAGE
                // ========================================================

                var currentStage =
                    GetCurrentStage(
                        qc,
                        quarantine,
                        processing,
                        finalReceiving
                    );


                var status =
                    finalReceiving?.Status ==
                    "COMMITTED"
                        ? "COMPLETED"
                        : "IN_PROGRESS";


                // ========================================================
                // BRANCH
                // ========================================================

                var branchId =
                    incoming.BranchId?.Trim()
                    ?? "";


                var branchName =
                    !string.IsNullOrWhiteSpace(
                        branchId) &&
                    branches.TryGetValue(
                        branchId,
                        out var foundBranchName)
                            ? foundBranchName
                            : branchId;


                // ========================================================
                // SUPPLIER
                // ========================================================

                var supplierName =
                    suppliers.TryGetValue(
                        incoming.SupplierId,
                        out var foundSupplierName)
                            ? foundSupplierName
                            : "";


                result.Add(
                    new TimeInMotionListDto
                    {
                        IncomingReceivingId =
                            incoming.IncomingReceivingId,

                        IncomingNo =
                            incoming.IncomingNo,

                        PoNo =
                            qc?.PoNo ?? "",

                        SupplierName =
                            supplierName,

                        BranchId =
                            branchId,

                        BranchName =
                            branchName,

                        QcId =
                            qc?.QcId,

                        QcNo =
                            qc?.QcNo,

                        QuarantineId =
                            quarantine?.QuarantineId,

                        QuarantineNo =
                            quarantine?.QuarantineNo,

                        ProcessingId =
                            processing?.ProcessingId,

                        ProcessingNo =
                            processing?.ProcessingNo,

                        FinalRrId =
    finalReceiving?.RrId,

                        FinalRrNo =
    finalReceiving?.RrNo,

                        StartedAt =
                            startedAt,

                        CompletedAt =
                            completedAt,

                        TotalDurationMinutes =
                            GetDurationMinutes(
                                startedAt,
                                completedAt
                            ),

                        CurrentStage =
                            currentStage,

                        Status =
                            status,

                        Stages =
                            stages
                    }
                );
            }


            return result;
        }


        // ============================================================
        // CURRENT STAGE
        // ============================================================

        private static string GetCurrentStage(
        Models.Purchasing.QcInspections.QcInspectionHeader? qc,
        Models.Purchasing.Quarantine.QuarantineHeader? quarantine,
        Models.Purchasing.RawMaterialProcessing.RmwProcessingHeader? processing,
        Models.Purchasing.ReceivingReports.ReceivingReportHeader? finalReceiving)
        {
            if (
                finalReceiving?.Status ==
                "COMMITTED"
            )
            {
                return "Inventory Committed";
            }


            if (finalReceiving != null)
            {
                return "Final Receiving";
            }


            if (
                processing?.Status ==
                "READY_FOR_FINAL_RR"
            )
            {
                return "Ready for Final RR";
            }


            if (processing != null)
            {
                return "Raw Material Processing";
            }


            if (
                quarantine?.Status ==
                "RELEASED"
            )
            {
                return "Released to RMW";
            }


            if (quarantine != null)
            {
                return "Quarantine / Hold";
            }


            if (qc != null)
            {
                return "QA/QC Inspection";
            }


            return "Incoming Receiving";
        }


        // ============================================================
        // DURATION
        // ============================================================

        private static double? GetDurationMinutes(
            DateTime? start,
            DateTime? end)
        {
            if (
                !start.HasValue ||
                !end.HasValue
            )
            {
                return null;
            }

            var duration =
                end.Value -
                start.Value;

            if (duration.TotalMinutes < 0)
            {
                return null;
            }

            return Math.Round(
                duration.TotalMinutes,
                2
            );
        }
    }
}