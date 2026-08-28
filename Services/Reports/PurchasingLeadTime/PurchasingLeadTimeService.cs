using inventory_api.Data;
using inventory_api.DTOs.Reports.PurchasingLeadTime;
using Microsoft.EntityFrameworkCore;

namespace inventory_api.Services.Reports.PurchasingLeadTime
{
    public class PurchasingLeadTimeService
    {
        private readonly AppDbContext _context;

        public PurchasingLeadTimeService(
            AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<PurchasingLeadTimeDto>>
            GetAllAsync()
        {
            // ==========================================
            // USERS
            // ==========================================

            var users =
                await _context.Users
                    .AsNoTracking()
                    .ToDictionaryAsync(
                        x => x.user_id,
                        x => string.IsNullOrWhiteSpace(
                            x.full_name)
                                ? x.username
                                : x.full_name
                    );


            string GetUserName(
                string? userId)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return "";

                return users.TryGetValue(
                    userId,
                    out var name)
                        ? name
                        : userId;
            }


            // ==========================================
            // MPRF → CANVASS → PO
            // ==========================================

            var records =
                await (
                    from mprf in
                        _context.PurchasingMprfHeaders
                            .AsNoTracking()

                    join canvass in
                        _context.PurchasingCanvassHeaders
                            .AsNoTracking()
                        on mprf.mprf_id
                        equals canvass.MprfId

                    join po in
                        _context.PurchaseOrderHeaders
                            .AsNoTracking()
                        on canvass.CanvassId
                        equals po.CanvassId

                    join supplier in
                        _context.Suppliers
                            .AsNoTracking()
                        on po.SupplierId
                        equals supplier.SupplierId

                    where po.Status != "CANCELLED"

                    orderby po.PoId descending

                    select new
                    {
                        Mprf =
                            mprf,

                        Po =
                            po,

                        SupplierName =
                            supplier.SupplierName
                    }
                )
                .ToListAsync();


            var result =
                new List<PurchasingLeadTimeDto>();


            foreach (var record in records)
            {
                var mprf =
                    record.Mprf;

                var po =
                    record.Po;


                // ======================================
                // FIRST ACTUAL INCOMING RECEIVING
                // ======================================

                var incoming =
                    await _context.IncomingReceivings
                        .AsNoTracking()
                        .Where(x =>
                            x.PoId == po.PoId &&
                            x.ReceivingStatus !=
                                "NOT_ACCEPTED")
                        .OrderBy(x =>
                            x.CreatedAt)
                        .ThenBy(x =>
                            x.IncomingReceivingId)
                        .FirstOrDefaultAsync();


                var submittedAt =
                    mprf.submitted_at;

                var approvedAt =
                    po.ApprovedAt;

                var receivedAt =
                    incoming?.CreatedAt;


                result.Add(
                    new PurchasingLeadTimeDto
                    {
                        MprfId =
                            mprf.mprf_id,

                        MprfNo =
                            mprf.mprf_no ?? "",

                        Category =
                            mprf.category,

                        RequestedBy =
                            mprf.requested_by,

                        RequestedByName =
                            GetUserName(
                                mprf.requested_by),

                        SubmittedBy =
                            mprf.submitted_by,

                        SubmittedByName =
                            GetUserName(
                                mprf.submitted_by),

                        MprfSubmittedAt =
                            submittedAt,


                        PoId =
                            po.PoId,

                        PoNo =
                            po.PoNo,

                        SupplierId =
                            po.SupplierId,

                        SupplierName =
                            record.SupplierName,

                        PoStatus =
                            po.Status,

                        ApprovedBy =
                            po.ApprovedBy,

                        ApprovedByName =
                            GetUserName(
                                po.ApprovedBy),

                        PoApprovedAt =
                            approvedAt,


                        IncomingReceivingId =
                            incoming?
                                .IncomingReceivingId,

                        IncomingNo =
                            incoming?
                                .IncomingNo,

                        IncomingReceivedAt =
                            receivedAt,

                        IncomingReceivedBy =
    incoming?.CreatedBy,

                        IncomingReceivedByName =
    GetUserName(
        incoming?.CreatedBy),


                        MprfReviewedAt =
    mprf.reviewed_at,

                        ReviewedBy =
    mprf.reviewed_by,

                        ReviewedByName =
    GetUserName(
        mprf.reviewed_by),

                        MprfReviewMinutes =
    GetDurationMinutes(
        mprf.submitted_at,
        mprf.reviewed_at),

                        PoProcessingMinutes =
    GetDurationMinutes(
        mprf.reviewed_at,
        po.ApprovedAt),


                        // MPRF SUBMITTED
                        //      ↓
                        // PO APPROVED
                        PurchasingLeadTimeMinutes =
                            GetDurationMinutes(
                                submittedAt,
                                approvedAt),


                        // PO APPROVED
                        //      ↓
                        // INCOMING RECEIVING
                        DeliveryLeadTimeMinutes =
                            GetDurationMinutes(
                                approvedAt,
                                receivedAt),


                        // MPRF SUBMITTED
                        //      ↓
                        // INCOMING RECEIVING
                        OverallLeadTimeMinutes =
                            GetDurationMinutes(
                                submittedAt,
                                receivedAt),


                        CurrentStage =
                            GetCurrentStage(
                                submittedAt,
                                approvedAt,
                                receivedAt)
                    }
                );
            }


            return result;
        }


        private static double? GetDurationMinutes(
            DateTime? start,
            DateTime? end)
        {
            if (!start.HasValue ||
                !end.HasValue)
            {
                return null;
            }

            var duration =
                end.Value -
                start.Value;

            if (duration.TotalMinutes < 0)
                return null;

            return Math.Round(
                duration.TotalMinutes,
                2);
        }


        private static string GetCurrentStage(
            DateTime? submittedAt,
            DateTime? approvedAt,
            DateTime? receivedAt)
        {
            if (receivedAt.HasValue)
                return "Received";

            if (approvedAt.HasValue)
                return "Waiting for Delivery";

            if (submittedAt.HasValue)
                return "Purchasing Processing";

            return "Submission Timestamp Missing";
        }
    }
}