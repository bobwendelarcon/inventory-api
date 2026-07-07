    using inventory_api.DTOs;
using MySqlConnector;
using System.Data;

namespace inventory_api.Services
{
    public class DeliveryChecklistService
    {
        private readonly string _connectionString;
        private readonly InventoryTransactionService _inventoryTransactionService;
        public DeliveryChecklistService(
     IConfiguration configuration,
     InventoryTransactionService inventoryTransactionService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string not found.");

            _inventoryTransactionService = inventoryTransactionService;
        }
        private class AllocationLotItem
        {
            public string lot_no { get; set; } = string.Empty;
            public string? branch_id { get; set; }   // ✅ NEW
            public DateTime? manufacturing_date { get; set; }
            public DateTime? expiration_date { get; set; }
            public decimal allocated_qty { get; set; }
        }

        private class ChecklistLineSnapshot
        {
            public long checklist_id { get; set; }
            public long order_id { get; set; }
            public string order_no { get; set; } = "";
            public long order_line_id { get; set; }
            public string? customer_id { get; set; }
            public string? customer_name { get; set; }
            public string product_id { get; set; } = "";
            public string product_name { get; set; } = "";
            public string? uom { get; set; }
            public string? pack_uom { get; set; }
            public decimal? pack_qty { get; set; }
            public decimal required_qty { get; set; }
            public decimal checklist_qty { get; set; }
            public string status { get; set; } = "";
            public string branch_id { get; set; } = "";
            public string lot_no { get; set; } = "";
        }

        private class ChecklistLineCompletionData
        {
            public long checklist_line_id { get; set; }
            public string product_id { get; set; } = "";
            public string? branch_id { get; set; }
            public string? lot_no { get; set; }
            public decimal quantity { get; set; }
        }

        public async Task<object> CreateChecklistAsync(CreateChecklistDto dto, string createdBy)
        {
            ValidateCreateChecklist(dto);

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                string checklistNo = await GenerateChecklistNoAsync(conn, transaction);

                long checklistId = await InsertChecklistHeaderAsync(
                    conn,
                    transaction,
                    dto,
                    checklistNo,
                    createdBy
                );

                foreach (var line in dto.lines)
                {
                    await ValidateChecklistLineAsync(conn, transaction, line);

                    var allocationLots = await GetAllocationLotsAsync(
                        conn,
                        transaction,
                        line.order_line_id
                    );

                    if (allocationLots.Count == 0)
                        throw new Exception($"No allocated lots found for order line {line.order_line_id}.");

                    decimal totalLotAllocatedQty = allocationLots.Sum(x => x.allocated_qty);

                    if (totalLotAllocatedQty <= 0)
                        throw new Exception($"Allocated lot qty is zero for order line {line.order_line_id}.");

                    // ✅ MULTI-WAREHOUSE SUPPORTED
                    // Create one checklist line per allocated lot/warehouse.
                    foreach (var lot in allocationLots)
                    {
                        await InsertChecklistLinePerLotAsync(
                            conn,
                            transaction,
                            checklistId,
                            line,
                            lot
                        );
                    }
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Delivery checklist created successfully.",
                    checklist_id = checklistId,
                    checklist_no = checklistNo
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<List<AllocationLotItem>> GetAllocationLotsAsync(
      MySqlConnection conn,
      MySqlTransaction transaction,
      long orderLineId)
        {
            var result = new List<AllocationLotItem>();

            string sql = @"
        SELECT
            lot_no,
            branch_id,
            manufacturing_date,
            expiration_date,
            allocated_qty
        FROM daily_order_allocation
        WHERE order_line_id = @order_line_id
          AND allocated_qty > 0
        ORDER BY priority_rank ASC, expiration_date ASC;";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@order_line_id", orderLineId);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new AllocationLotItem
                {
                    lot_no = reader["lot_no"]?.ToString() ?? string.Empty,
                    branch_id = reader["branch_id"] == DBNull.Value ? null : reader["branch_id"]?.ToString(),
                    manufacturing_date = reader["manufacturing_date"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["manufacturing_date"]),
                    expiration_date = reader["expiration_date"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(reader["expiration_date"]),
                    allocated_qty = reader["allocated_qty"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(reader["allocated_qty"])
                });
            }

            return result;
        }

        private async Task InsertChecklistLinePerLotAsync(
      MySqlConnection conn,
      MySqlTransaction transaction,
      long checklistId,
      ChecklistLineDto line,
      AllocationLotItem lot)
        {


            string? resolvedCustomerId = null;

            if (!string.IsNullOrWhiteSpace(line.customer_id))
            {
                resolvedCustomerId = line.customer_id;
            }
            else
            {
                resolvedCustomerId = await GetCustomerIdByNameAsync(conn, transaction, line.customer_name);
            }



            string sql = @"
    INSERT INTO delivery_checklist_line
    (
        checklist_id,
        order_id,
        order_no,
        order_line_id,
        customer_id,
        customer_name,
        product_id,
        product_name,
        branch_id,
        lot_no,
        manufacturing_date,
        expiration_date,
        uom,
        pack_uom,
        pack_qty,
        required_qty,
        allocated_qty,
        checklist_qty,
        released_qty,
        remaining_qty,
        status,
        remarks,
        created_at,
        updated_at
    )
    VALUES
    (
        @checklist_id,
        @order_id,
        @order_no,
        @order_line_id,
        @customer_id,
        @customer_name,
        @product_id,
        @product_name,
        @branch_id,
        @lot_no,
        @manufacturing_date,
        @expiration_date,
        @uom,
        @pack_uom,
        @pack_qty,
        @required_qty,
        @allocated_qty,
        @checklist_qty,
        @released_qty,
        @remaining_qty,
        @status,
        @remarks,
        @created_at,
        @updated_at
    );";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            var utcNow = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("@created_at", utcNow);
            cmd.Parameters.AddWithValue("@updated_at", utcNow);
            cmd.Parameters.AddWithValue("@checklist_id", checklistId);
            cmd.Parameters.AddWithValue("@order_id", line.order_id);
            cmd.Parameters.AddWithValue("@order_no", line.order_no);
            cmd.Parameters.AddWithValue("@order_line_id", line.order_line_id);
            cmd.Parameters.AddWithValue("@customer_id",
     string.IsNullOrWhiteSpace(resolvedCustomerId)
         ? DBNull.Value
         : resolvedCustomerId);
            cmd.Parameters.AddWithValue("@customer_name", (object?)line.customer_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@product_id", line.product_id);
            cmd.Parameters.AddWithValue("@product_name", line.product_name);
            cmd.Parameters.AddWithValue("@branch_id",
    string.IsNullOrWhiteSpace(lot.branch_id)
        ? DBNull.Value
        : lot.branch_id);
            cmd.Parameters.AddWithValue("@lot_no", lot.lot_no);
            cmd.Parameters.AddWithValue("@manufacturing_date", (object?)lot.manufacturing_date ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@expiration_date", (object?)lot.expiration_date ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@uom", (object?)line.uom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pack_uom", (object?)line.pack_uom ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pack_qty", (object?)line.pack_qty ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@required_qty", line.required_qty);
            cmd.Parameters.AddWithValue("@allocated_qty", lot.allocated_qty);
            cmd.Parameters.AddWithValue("@checklist_qty", lot.allocated_qty);
            cmd.Parameters.AddWithValue("@released_qty", 0);
            cmd.Parameters.AddWithValue("@remaining_qty", lot.allocated_qty);
            cmd.Parameters.AddWithValue("@status", "READY");
            cmd.Parameters.AddWithValue("@remarks", DBNull.Value);

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows <= 0)
                throw new Exception($"Failed to insert checklist line for product {line.product_name}, lot {lot.lot_no}.");
        }

        private void ValidateCreateChecklist(CreateChecklistDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            if (dto.delivery_date == default)
                throw new Exception("delivery_date is required.");

            if (dto.lines == null || dto.lines.Count == 0)
                throw new Exception("At least one checklist line is required.");
        }

        private async Task<string> GenerateChecklistNoAsync(MySqlConnection conn, MySqlTransaction transaction)
        {
            string sql = @"
                SELECT checklist_no
                FROM delivery_checklist_header
                ORDER BY checklist_id DESC
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            var result = await cmd.ExecuteScalarAsync();

            int nextNumber = 1;

            if (result != null && result != DBNull.Value)
            {
                string lastChecklistNo = result.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(lastChecklistNo) && lastChecklistNo.StartsWith("DC-"))
                {
                    string numericPart = lastChecklistNo.Replace("DC-", "");
                    if (int.TryParse(numericPart, out int parsed))
                    {
                        nextNumber = parsed + 1;
                    }
                }
            }

            return $"DC-{nextNumber:D4}";
        }

        private async Task<long> InsertChecklistHeaderAsync(
    MySqlConnection conn,
    MySqlTransaction transaction,
    CreateChecklistDto dto,
    string checklistNo,
    string createdBy)
        {
            string sql = @"
    INSERT INTO delivery_checklist_header
    (
        checklist_no,
        delivery_date,
        route_name,
        truck_name,
        driver_name,
        helper_name,
        status,
        remarks,
        created_by,
        created_at,
        updated_at
    )
    VALUES
    (
        @checklist_no,
        @delivery_date,
        @route_name,
        @truck_name,
        @driver_name,
        @helper_name,
        @status,
        @remarks,
        @created_by,
        @created_at,
        @updated_at
    );

    SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            var utcNow = DateTime.UtcNow;
            cmd.Parameters.AddWithValue("@created_at", utcNow);
            cmd.Parameters.AddWithValue("@updated_at", utcNow);
            cmd.Parameters.AddWithValue("@checklist_no", checklistNo);
            cmd.Parameters.AddWithValue("@delivery_date", dto.delivery_date);
            cmd.Parameters.AddWithValue("@route_name", (object?)dto.route_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@truck_name", (object?)dto.truck_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@driver_name", (object?)dto.driver_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@helper_name", (object?)dto.helper_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", "READY");
            cmd.Parameters.AddWithValue("@remarks", (object?)dto.remarks ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy);

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                throw new Exception("Failed to create checklist header.");

            return Convert.ToInt64(result);
        }

        private async Task ValidateChecklistLineAsync(
            MySqlConnection conn,
            MySqlTransaction transaction,
            ChecklistLineDto line)
        {
            if (line.order_id <= 0)
                throw new Exception("Invalid order_id.");

            if (line.order_line_id <= 0)
                throw new Exception("Invalid order_line_id.");

            if (string.IsNullOrWhiteSpace(line.order_no))
                throw new Exception("order_no is required.");

            if (string.IsNullOrWhiteSpace(line.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(line.product_name))
                throw new Exception("product_name is required.");

            if (line.checklist_qty <= 0)
                throw new Exception($"Checklist qty must be greater than 0 for product {line.product_name}.");

            string sql = @"
                SELECT 
                    allocated_qty,
                    required_qty,
                    allocation_status
                FROM daily_order_line
                WHERE order_line_id = @order_line_id
                  AND order_id = @order_id
                LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@order_line_id", line.order_line_id);
            cmd.Parameters.AddWithValue("@order_id", line.order_id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new Exception($"Order line not found. order_line_id={line.order_line_id}");

            decimal dbAllocatedQty = reader["allocated_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["allocated_qty"]);
            decimal dbRequiredQty = reader["required_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["required_qty"]);
            string allocationStatus = reader["allocation_status"]?.ToString() ?? "";

            await reader.CloseAsync();

            if (dbAllocatedQty <= 0)
                throw new Exception($"Order line {line.order_line_id} has no allocated qty.");

            if (line.checklist_qty > dbAllocatedQty)
                throw new Exception(
                    $"Checklist qty cannot be greater than allocated qty. Product: {line.product_name}, Allocated: {dbAllocatedQty}, Requested: {line.checklist_qty}");

            var status = allocationStatus?.Trim().ToUpper();

            if (status != "ALLOCATED" &&
                status != "PARTIALLY ALLOCATED" &&
                 status != "PARTIAL" &&
                status != "FULLY ALLOCATED")
            {
                throw new Exception($"Order line {line.order_line_id} is not ready for checklist. Status: {allocationStatus}");
            }
        }

    



        public async Task<List<ReadyForChecklistDto>> GetReadyForChecklistAsync()
        {
            var result = new List<ReadyForChecklistDto>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            ////this is for allowing allocated to be dispatch
            //        string sql = @"


            string sql = @"
SELECT 
    ol.order_line_id,
    ol.order_id,
    oh.order_no,
    oh.customer_name,
    oh.route_name,
    oh.delivery_date,
    ol.product_id,
    ol.product_name,
    p.product_description,
    p.uom,
    p.pack_uom,
    p.pack_qty,

    GREATEST(IFNULL(ol.required_qty, 0) - IFNULL(ol.dispatched_qty, 0), 0) AS remaining_order_qty,
    ol.allocated_qty AS checklist_qty,

    ol.required_qty,
    ol.allocated_qty,
    ol.dispatched_qty,
    ol.remaining_qty,
    ol.allocation_status
FROM daily_order_line ol
INNER JOIN daily_order_header oh 
    ON ol.order_id = oh.order_id
LEFT JOIN products p
    ON ol.product_id = p.product_id
WHERE 
    IFNULL(oh.is_deleted, 0) = 0
    AND ol.allocated_qty > 0
    AND UPPER(TRIM(oh.status)) = 'READY FOR DISPATCH'
   AND NOT EXISTS
(
    SELECT 1
    FROM delivery_checklist_line dcl
    INNER JOIN delivery_checklist_header dch
        ON dcl.checklist_id = dch.checklist_id
    WHERE 
        dcl.order_line_id = ol.order_line_id
        AND IFNULL(dcl.is_deleted, 0) = 0
        AND IFNULL(dch.is_deleted, 0) = 0
        AND UPPER(TRIM(dch.status)) IN 
        (
            'READY',
            'LOADING',
            'PARTIAL',
            'PARTIALLY_COMPLETED',
            'COMPLETED'
        )
)
ORDER BY oh.delivery_date ASC, oh.order_no ASC, ol.order_line_id ASC;";

            /// string sql = @"
            //            //SELECT 
            //            //    ol.order_line_id,
            //            //    ol.order_id,
            //            //    oh.order_no,
            //            //    oh.customer_name,
            //            //    oh.route_name,
            //            //    oh.delivery_date,
            //            //    ol.product_id,
            //            //    ol.product_name,
            //            //    ol.required_qty,
            //            //    ol.allocated_qty,
            //            //    ol.remaining_qty,
            //            //    ol.allocation_status
            //            //FROM daily_order_line ol
            //            //INNER JOIN daily_order_header oh 
            //            //    ON ol.order_id = oh.order_id
            //            //WHERE 
            //            //    IFNULL(oh.is_deleted, 0) = 0
            //            //    AND ol.allocated_qty > 0
            //            //    AND UPPER(TRIM(oh.status)) = 'READY FOR DISPATCH'
            //            //    AND NOT EXISTS
            //            //    (
            //            //        SELECT 1
            //            //        FROM delivery_checklist_line dcl
            //            //        INNER JOIN delivery_checklist_header dch
            //            //            ON dcl.checklist_id = dch.checklist_id
            //            //        WHERE dcl.order_line_id = ol.order_line_id
            //            //          AND IFNULL(dcl.is_deleted, 0) = 0
            //            //          AND IFNULL(dch.is_deleted, 0) = 0
            //            //    )
            //            ORDER BY oh.delivery_date ASC, oh.order_no ASC, ol.order_line_id ASC;";



            using var cmd = new MySqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                decimal allocatedQty = reader["allocated_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["allocated_qty"]);

                result.Add(new ReadyForChecklistDto
                {
                    order_line_id = Convert.ToInt64(reader["order_line_id"]),
                    order_id = Convert.ToInt64(reader["order_id"]),
                    order_no = reader["order_no"]?.ToString() ?? "",
                  //  customer_id = reader["customer_id"] == DBNull.Value ? null : reader["customer_id"]?.ToString(),
                    customer_name = reader["customer_name"]?.ToString() ?? "",
                    route_name = reader["route_name"] == DBNull.Value ? null : reader["route_name"]?.ToString(),
                    delivery_date = reader["delivery_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["delivery_date"]),
                    product_id = reader["product_id"]?.ToString() ?? "",
                    product_name = reader["product_name"]?.ToString() ?? "",
                    product_description = reader["product_description"] == DBNull.Value
    ? null
    : reader["product_description"].ToString(),
                    uom = reader["uom"] == DBNull.Value ? null : reader["uom"]?.ToString(),
                    pack_uom = reader["pack_uom"] == DBNull.Value ? null : reader["pack_uom"]?.ToString(),
                    pack_qty = reader["pack_qty"] == DBNull.Value ? null : Convert.ToDecimal(reader["pack_qty"]),
                    required_qty = reader["remaining_order_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["remaining_order_qty"]),
                    allocated_qty = reader["checklist_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["checklist_qty"]),
                    remaining_qty = reader["remaining_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["remaining_qty"]),
                    dispatched_qty = reader["dispatched_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["dispatched_qty"]),
                    allocation_status = reader["allocation_status"]?.ToString() ?? "",
                    available_for_checklist = allocatedQty
                });
            }

            return result;
        }



        public async Task<List<object>> GetChecklistListAsync(
    DateTime? date,
    string? status,
    string? truck,
    string? search)
        {
            status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            truck = string.IsNullOrWhiteSpace(truck) ? null : truck.Trim();
            search = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
        SELECT
            h.checklist_id,
            h.checklist_no,
            h.delivery_date,
            h.route_name,
            h.truck_name,
            h.driver_name,
            h.status,
        h.created_by AS createdBy,
       (
    SELECT GROUP_CONCAT(DISTINCT it.dr_no ORDER BY it.dr_no SEPARATOR ', ')
    FROM inventory_transactions it
    WHERE it.checklist_id = h.checklist_id
      AND IFNULL(it.is_deleted,0)=0
      AND IFNULL(it.dr_no,'') <> ''
) AS dr_numbers,
            
            COUNT(DISTINCT l.customer_name) AS total_customers
        FROM delivery_checklist_header h
        LEFT JOIN delivery_checklist_line l
            ON h.checklist_id = l.checklist_id
            AND IFNULL(l.is_deleted, 0) = 0
        LEFT JOIN users u
            ON h.created_by = u.user_id


        WHERE IFNULL(h.is_deleted, 0) = 0
          AND (@date IS NULL OR DATE(h.delivery_date) = @date)
          AND (@status IS NULL OR UPPER(h.status) = UPPER(@status))
          AND (@truck IS NULL OR h.truck_name LIKE CONCAT('%', @truck, '%'))
          AND (
                @search IS NULL
                OR h.checklist_no LIKE CONCAT('%', @search, '%')
                OR h.route_name LIKE CONCAT('%', @search, '%')
                OR h.truck_name LIKE CONCAT('%', @search, '%')
                OR h.driver_name LIKE CONCAT('%', @search, '%')
                OR u.full_name LIKE CONCAT('%', @search, '%')
OR EXISTS
(
    SELECT 1
    FROM inventory_transactions it
    WHERE it.checklist_id = h.checklist_id
      AND IFNULL(it.is_deleted,0)=0
      AND it.dr_no LIKE CONCAT('%', @search, '%')
)
              )
        GROUP BY
            h.checklist_id,
            h.checklist_no,
            h.delivery_date,
            h.route_name,
            h.truck_name,
            h.driver_name,
            h.status,
            h.created_by,
            u.full_name
        ORDER BY h.delivery_date DESC, h.checklist_id DESC;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@date", date.HasValue ? date.Value.Date : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@truck", (object?)truck ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@search", (object?)search ?? DBNull.Value);

            var list = new List<object>();

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    checklist_id = reader["checklist_id"] == DBNull.Value ? 0 : Convert.ToInt64(reader["checklist_id"]),
                    checklist_no = reader["checklist_no"]?.ToString() ?? "",
                    delivery_date = reader["delivery_date"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["delivery_date"]),
                    route_name = reader["route_name"] == DBNull.Value ? null : reader["route_name"]?.ToString(),
                    truck_name = reader["truck_name"] == DBNull.Value ? null : reader["truck_name"]?.ToString(),
                    driver_name = reader["driver_name"] == DBNull.Value ? null : reader["driver_name"]?.ToString(),
                    status = reader["status"] == DBNull.Value ? null : reader["status"]?.ToString(),
                    dr_numbers = reader["dr_numbers"] == DBNull.Value
    ? ""
    : reader["dr_numbers"].ToString(),
                    createdBy = reader["createdBy"] == DBNull.Value
        ? ""
        : reader["createdBy"].ToString(),
                    total_customers = reader["total_customers"] == DBNull.Value ? 0 : Convert.ToInt32(reader["total_customers"])
                });
            }

            return list;
        }


        private async Task<string?> GetCustomerIdByNameAsync(
            MySqlConnection conn,
            MySqlTransaction transaction,
            string? customerName)
        {
            if (string.IsNullOrWhiteSpace(customerName))
                return null;

            string sql = @"
        SELECT partner_id
        FROM partners
        WHERE UPPER(TRIM(partner_name)) = UPPER(TRIM(@customer_name))
          AND IFNULL(is_deleted, 0) = 0
        LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@customer_name", customerName.Trim());

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return null;

            return result.ToString();
        }
        public async Task<DeliveryChecklistDetailsDto> GetChecklistDetailsAsync(long checklistId)
        {
            if (checklistId <= 0)
                throw new Exception("Invalid checklist_id.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var result = new DeliveryChecklistDetailsDto();

            // HEADER
            string headerSql = @"
        SELECT
            checklist_id,
            checklist_no,
            delivery_date,
            route_name,
            truck_name,
            driver_name,
            helper_name,
            status,

            remarks
        FROM delivery_checklist_header
        WHERE checklist_id = @checklist_id
          AND is_deleted = 0
        LIMIT 1;";

            using (var headerCmd = new MySqlCommand(headerSql, conn))
            {
                headerCmd.Parameters.AddWithValue("@checklist_id", checklistId);

                using var reader = await headerCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    throw new Exception("Checklist not found.");

                result.checklist_id = Convert.ToInt64(reader["checklist_id"]);
                result.checklist_no = reader["checklist_no"]?.ToString() ?? string.Empty;
                result.delivery_date = reader["delivery_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["delivery_date"]);
                result.route_name = reader["route_name"] == DBNull.Value ? null : reader["route_name"]?.ToString();
                result.truck_name = reader["truck_name"] == DBNull.Value ? null : reader["truck_name"]?.ToString();
                result.driver_name = reader["driver_name"] == DBNull.Value ? null : reader["driver_name"]?.ToString();
                result.helper_name = reader["helper_name"] == DBNull.Value ? null : reader["helper_name"]?.ToString();
                result.status = reader["status"] == DBNull.Value ? null : reader["status"]?.ToString();
                result.remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"]?.ToString();
            }

            // LINES
            string lineSql = @"
SELECT
    dcl.checklist_line_id,
    dcl.order_id,
    dcl.order_no,
    dcl.order_line_id,
    dcl.customer_id,
    dcl.customer_name,
    dcl.product_id,
    dcl.product_name,
    p.product_description,

    dcl.branch_id,
    b.branch_name AS branch_name,

    dcl.lot_no,
    dcl.lot_no,

(
    SELECT GROUP_CONCAT(DISTINCT it.dr_no ORDER BY it.dr_no SEPARATOR ', ')
    FROM inventory_transactions it
    WHERE it.checklist_line_id = dcl.checklist_line_id
      AND IFNULL(it.is_deleted, 0) = 0
      AND IFNULL(it.dr_no, '') <> ''
) AS dr_no,

dcl.uom,
    dcl.pack_uom,
    dcl.pack_qty,

    dcl.manufacturing_date,
    dcl.expiration_date,
    dcl.required_qty,
    dcl.allocated_qty,
    dcl.checklist_qty,
    dcl.released_qty,
    dcl.remaining_qty,
    dcl.status,

    dcl.remarks
FROM delivery_checklist_line dcl
LEFT JOIN products p
    ON dcl.product_id = p.product_id
LEFT JOIN branches b
    ON dcl.branch_id = b.branch_id
WHERE dcl.checklist_id = @checklist_id
  AND dcl.is_deleted = 0
ORDER BY dcl.product_name ASC, dcl.expiration_date ASC, dcl.lot_no ASC;";

            using (var lineCmd = new MySqlCommand(lineSql, conn))
            {
                lineCmd.Parameters.AddWithValue("@checklist_id", checklistId);

                using var reader = await lineCmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.lines.Add(new DeliveryChecklistDetailsLineDto
                    {
                        checklist_line_id = Convert.ToInt64(reader["checklist_line_id"]),
                        order_id = Convert.ToInt64(reader["order_id"]),
                        order_no = reader["order_no"]?.ToString() ?? string.Empty,
                        order_line_id = Convert.ToInt64(reader["order_line_id"]),
                        customer_id = reader["customer_id"] == DBNull.Value ? null : reader["customer_id"]?.ToString(),
                        customer_name = reader["customer_name"] == DBNull.Value ? null : reader["customer_name"]?.ToString(),
                        product_id = reader["product_id"]?.ToString() ?? string.Empty,
                        product_name = reader["product_name"]?.ToString() ?? string.Empty,
                        product_description = reader["product_description"] == DBNull.Value
                        ? null
                        : reader["product_description"]?.ToString(),
                        branch_id = reader["branch_id"] == DBNull.Value ? null : reader["branch_id"]?.ToString(),
                        branch_name = reader["branch_name"] == DBNull.Value
                        ? null
                        : reader["branch_name"]?.ToString(),
                        lot_no = reader["lot_no"] == DBNull.Value ? null : reader["lot_no"]?.ToString(),
                        dr_no = reader["dr_no"] == DBNull.Value ? null : reader["dr_no"]?.ToString(),
                        manufacturing_date = reader["manufacturing_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["manufacturing_date"]),
                        expiration_date = reader["expiration_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["expiration_date"]),


                        uom = reader["uom"] == DBNull.Value ? null : reader["uom"]?.ToString(),
                        pack_uom = reader["pack_uom"] == DBNull.Value ? null : reader["pack_uom"]?.ToString(),
                        pack_qty = reader["pack_qty"] == DBNull.Value ? null : Convert.ToDecimal(reader["pack_qty"]),


                        required_qty = reader["required_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["required_qty"]),
                        allocated_qty = reader["allocated_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["allocated_qty"]),
                        checklist_qty = reader["checklist_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["checklist_qty"]),
                        released_qty = reader["released_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["released_qty"]),
                        remaining_qty = reader["remaining_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["remaining_qty"]),
                        status = reader["status"] == DBNull.Value ? null : reader["status"]?.ToString(),
                        remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"]?.ToString()
                    });
                }
            }

            return result;
        }


        public async Task DeleteChecklistAsync(long checklistId)
        {
            if (checklistId <= 0)
                throw new Exception("Invalid checklist_id.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                string checkSql = @"
            SELECT status
            FROM delivery_checklist_header
            WHERE checklist_id = @checklist_id
              AND IFNULL(is_deleted, 0) = 0
            LIMIT 1;";

                string? status;

                using (var checkCmd = new MySqlCommand(checkSql, conn, transaction))
                {
                    checkCmd.Parameters.AddWithValue("@checklist_id", checklistId);

                    var result = await checkCmd.ExecuteScalarAsync();

                    if (result == null)
                        throw new Exception("Checklist not found or already deleted.");

                    status = result.ToString();
                }

                if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase) &&
     !string.Equals(status, "LOADING", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Only READY or LOADING checklist line can be completed.");
                }

                string updateHeaderSql = @"
            UPDATE delivery_checklist_header
            SET is_deleted = 1,
                updated_at = @updated_at
            WHERE checklist_id = @checklist_id;";

                using (var headerCmd = new MySqlCommand(updateHeaderSql, conn, transaction))
                {
                    headerCmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    headerCmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await headerCmd.ExecuteNonQueryAsync();
                }


                string updateLineSql = @"
            UPDATE delivery_checklist_line
            SET is_deleted = 1,
                updated_at = @updated_at
            WHERE checklist_id = @checklist_id;";

                using (var lineCmd = new MySqlCommand(updateLineSql, conn, transaction))
                {
                    lineCmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    lineCmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await lineCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        //confirm loading

        public async Task ConfirmLoadingAsync(long checklistId)
        {
            if (checklistId <= 0)
                throw new Exception("Invalid checklist_id.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 1. Check current status
                string checkSql = @"
            SELECT status
            FROM delivery_checklist_header
            WHERE checklist_id = @checklist_id
              AND IFNULL(is_deleted, 0) = 0
            LIMIT 1;";

                string? status;

                using (var cmd = new MySqlCommand(checkSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", checklistId);

                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                        throw new Exception("Checklist not found.");

                    status = result.ToString();
                }

                // 2. Allow only READY
                if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Only READY checklist can be confirmed.");
                }

                // 3. Update status
                string updateSql = @"
    UPDATE delivery_checklist_header
    SET status = 'LOADING',
        updated_at = @updated_at
    WHERE checklist_id = @checklist_id;";

                using (var updateCmd = new MySqlCommand(updateSql, conn, transaction))
                {
                    updateCmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    updateCmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                string updateLinesSql = @"
    UPDATE delivery_checklist_line
    SET status = 'LOADING',
        updated_at = @updated_at
    WHERE checklist_id = @checklist_id
      AND IFNULL(is_deleted, 0) = 0
      AND IFNULL(remaining_qty, 0) > 0
      AND UPPER(IFNULL(status, '')) NOT IN ('COMPLETED');";

                using (var lineCmd = new MySqlCommand(updateLinesSql, conn, transaction))
                {
                    lineCmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    lineCmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await lineCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<object> CompleteLineAsync(CompleteChecklistLineDto dto, string completedBy)
        {
            if (dto.checklist_id <= 0)
                throw new Exception("Invalid checklist_id.");

            if (dto.checklist_line_id <= 0)
                throw new Exception("Invalid checklist_line_id.");

            if (dto.quantity <= 0)
                throw new Exception("Quantity must be greater than zero.");

            if (string.IsNullOrWhiteSpace(dto.dr_no))
                throw new Exception("DR No is required.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                string checklistNo = "";
                long orderId = 0;
                string orderNo = "";
                long orderLineId = 0;
                string productId = "";
                string productName = "";
                string? customerId = null;
                string customerName = "";
                string branchId = "";
                string lotNo = "";
                decimal checklistQty = 0;
                string status = "";

                string getLineSql = @"
SELECT
    h.checklist_no,
    l.order_id,
    l.order_no,
    l.order_line_id,
    l.product_id,
    l.product_name,
    l.customer_id,
    l.customer_name,
    l.branch_id,
    l.lot_no,
    l.checklist_qty,
    l.status
FROM delivery_checklist_line l
INNER JOIN delivery_checklist_header h
    ON l.checklist_id = h.checklist_id
WHERE l.checklist_id = @checklist_id
  AND l.checklist_line_id = @checklist_line_id
  AND IFNULL(l.is_deleted, 0) = 0
LIMIT 1;";

                using (var cmd = new MySqlCommand(getLineSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", dto.checklist_id);
                    cmd.Parameters.AddWithValue("@checklist_line_id", dto.checklist_line_id);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new Exception("Checklist line not found.");

                    checklistNo = reader["checklist_no"]?.ToString() ?? "";
                    orderId = Convert.ToInt64(reader["order_id"]);
                    orderNo = reader["order_no"]?.ToString() ?? "";
                    orderLineId = Convert.ToInt64(reader["order_line_id"]);
                    productId = reader["product_id"]?.ToString() ?? "";
                    productName = reader["product_name"]?.ToString() ?? "";
                    customerId = reader["customer_id"] == DBNull.Value ? null : reader["customer_id"]?.ToString();
                    customerName = reader["customer_name"]?.ToString() ?? "";
                    branchId = reader["branch_id"]?.ToString() ?? "";
                    lotNo = reader["lot_no"]?.ToString() ?? "";
                    checklistQty = reader["checklist_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["checklist_qty"]);
                    status = reader["status"]?.ToString() ?? "";
                }

                if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Only READY checklist line can be completed.");

                if (dto.quantity > checklistQty)
                    throw new Exception("Quantity cannot be greater than checklist quantity.");

                string finalRemarks = dto.remarks ?? "";


                await _inventoryTransactionService.AddAsync(new CreateInventoryTransactionDto
                {
                    product_id = productId,
                    branch_id = branchId,
                    transaction_type = "OUT",
                    lot_no = lotNo,
                    quantity = (double)dto.quantity,
                    scanned_by = completedBy,

                    customer_id = customerId,

                    dr_no = dto.dr_no ?? "",
                    inv_no = dto.inv_no ?? "",
                    po_no = dto.po_no ?? "",

                    checklist_id = dto.checklist_id,
                    checklist_no = checklistNo,
                    checklist_line_id = dto.checklist_line_id,

                    order_id = orderId,
                    order_no = orderNo,
                    order_line_id = orderLineId,

                    remarks = finalRemarks
                });

                string updateLineSql = @"
UPDATE delivery_checklist_line
SET status = 'COMPLETED',
    released_qty = @quantity,
    remaining_qty = 0,
    remarks = @remarks,
    updated_at = @updated_at
WHERE checklist_line_id = @checklist_line_id
  AND checklist_id = @checklist_id
  AND IFNULL(is_deleted, 0) = 0;";

                using (var cmd = new MySqlCommand(updateLineSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", dto.checklist_id);
                    cmd.Parameters.AddWithValue("@checklist_line_id", dto.checklist_line_id);
                    cmd.Parameters.AddWithValue("@quantity", dto.quantity);
                    cmd.Parameters.AddWithValue("@remarks", finalRemarks);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }


                string updateOrderLineSql = @"
UPDATE daily_order_line
SET dispatched_qty = IFNULL(dispatched_qty, 0) + @quantity,
    allocated_qty = GREATEST(IFNULL(allocated_qty, 0) - @quantity, 0),
    remaining_qty = GREATEST(IFNULL(required_qty, 0) - (IFNULL(dispatched_qty, 0) + @quantity), 0),
    allocation_status = CASE
        WHEN (IFNULL(dispatched_qty, 0) + @quantity) >= IFNULL(required_qty, 0)
            THEN 'Completed'
        WHEN GREATEST(IFNULL(allocated_qty, 0) - @quantity, 0) <= 0
            THEN 'Not Allocated'
        ELSE 'Partial'
    END,
    status = CASE
        WHEN (IFNULL(dispatched_qty, 0) + @quantity) >= IFNULL(required_qty, 0)
           THEN 'COMPLETED'
WHEN (IFNULL(dispatched_qty,0) + @quantity) > 0
THEN 'PARTIALLY DELIVERED'
ELSE 'READY FOR DISPATCH'
    END,
    updated_at = @updated_at
WHERE order_line_id = @order_line_id
  AND order_id = @order_id;";

                using (var cmd = new MySqlCommand(updateOrderLineSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@order_id", orderId);
                    cmd.Parameters.AddWithValue("@order_line_id", orderLineId);
                    cmd.Parameters.AddWithValue("@quantity", dto.quantity);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows <= 0)
                        throw new Exception($"Daily order line not updated. order_id={orderId}, order_line_id={orderLineId}");
                }



                string updateAllocationSql = @"
UPDATE daily_order_allocation
SET allocated_qty = GREATEST(IFNULL(allocated_qty, 0) - @quantity, 0)
WHERE order_line_id = @order_line_id
  AND product_id = @product_id
  AND branch_id = @branch_id
  AND lot_no = @lot_no
  AND allocated_qty > 0;";

                using (var cmd = new MySqlCommand(updateAllocationSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@order_line_id", orderLineId);
                    cmd.Parameters.AddWithValue("@product_id", productId);
                    cmd.Parameters.AddWithValue("@branch_id", branchId);
                    cmd.Parameters.AddWithValue("@lot_no", lotNo);
                    cmd.Parameters.AddWithValue("@quantity", dto.quantity);

                    await cmd.ExecuteNonQueryAsync();
                }


                string updateOrderHeaderSql = @"
UPDATE daily_order_header h
INNER JOIN delivery_checklist_header dch
    ON dch.checklist_id = @checklist_id
SET h.status = CASE
    WHEN NOT EXISTS (
        SELECT 1
        FROM daily_order_line l
        WHERE l.order_id = h.order_id
          AND IFNULL(l.dispatched_qty, 0) < IFNULL(l.required_qty, 0)
    )
    THEN 'COMPLETED'
    ELSE 'PARTIALLY DELIVERED'
END,
h.date_delivered = CASE
    WHEN NOT EXISTS (
        SELECT 1
        FROM daily_order_line l
        WHERE l.order_id = h.order_id
          AND IFNULL(l.dispatched_qty, 0) < IFNULL(l.required_qty, 0)
    )
    THEN dch.delivery_date
    ELSE h.date_delivered
END,
h.updated_at = @updated_at
WHERE h.order_id = @order_id;";

                using (var cmd = new MySqlCommand(updateOrderHeaderSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@order_id", orderId);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@checklist_id", dto.checklist_id);
                    await cmd.ExecuteNonQueryAsync();
                }



                string pendingSql = @"
SELECT COUNT(*)
FROM delivery_checklist_line
WHERE checklist_id = @checklist_id
  AND IFNULL(is_deleted, 0) = 0
  AND UPPER(TRIM(status)) <> 'COMPLETED';";

                long pendingCount;

                using (var cmd = new MySqlCommand(pendingSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", dto.checklist_id);
                    pendingCount = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                string headerStatus =
     pendingCount == 0
         ? "COMPLETED"
         : "PARTIALLY_COMPLETED";

                string updateHeaderSql = @"
UPDATE delivery_checklist_header
SET status = @status,
    updated_at = @updated_at
WHERE checklist_id = @checklist_id;";

                using (var cmd = new MySqlCommand(updateHeaderSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", dto.checklist_id);
                    cmd.Parameters.AddWithValue("@status", headerStatus);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Checklist line completed and inventory deducted."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<object> CompleteCustomerAsync(
    CompleteChecklistCustomerDto dto,
    string completedBy)
        {
            if (dto.checklist_id <= 0)
                throw new Exception("Invalid checklist.");

            if (string.IsNullOrWhiteSpace(dto.customer_name))
                throw new Exception("Customer is required.");

            if (string.IsNullOrWhiteSpace(dto.dr_no))
                throw new Exception("DR No is required.");

            var lines = await GetReadyChecklistLinesByCustomerAsync(
                dto.checklist_id,
                dto.customer_name);

            if (lines.Count == 0)
                throw new Exception("No READY lines found for this customer.");

            foreach (var line in lines)
            {
                var single = new CompleteChecklistLineDto
                {
                    checklist_id = dto.checklist_id,
                    checklist_line_id = line.checklist_line_id,

                    product_id = line.product_id,
                    branch_id = line.branch_id,
                    lot_no = line.lot_no,
                    quantity = line.quantity,

                    adjustment_type = "DEDUCT",
                    adjusted_by = completedBy,
                    reference_type = "DELIVERY_CHECKLIST",

                    dr_no = dto.dr_no,
                    inv_no = dto.inv_no,
                    po_no = dto.po_no,
                    remarks = dto.remarks
                };

                await CompleteLineAsync(single, completedBy);
            }

            return new
            {
                success = true,
                message = $"{lines.Count} line(s) completed for {dto.customer_name}."
            };
        }

        public async Task<object> CompleteLinesAsync(CompleteChecklistLinesDto dto, string completedBy)
        {
            if (dto.checklist_id <= 0)
                throw new Exception("Invalid checklist.");

            if (dto.checklist_line_ids == null || dto.checklist_line_ids.Count == 0)
                throw new Exception("Please select at least one checklist line.");

            if (string.IsNullOrWhiteSpace(dto.dr_no))
                throw new Exception("DR No is required.");

            var completedCount = 0;

            foreach (var lineId in dto.checklist_line_ids.Distinct())
            {
                var line = await GetChecklistLineForCompletionAsync(dto.checklist_id, lineId);

                var singleDto = new CompleteChecklistLineDto
                {
                    checklist_id = dto.checklist_id,
                    checklist_line_id = line.checklist_line_id,

                    product_id = line.product_id,
                    lot_no = line.lot_no,
                    branch_id = line.branch_id,

                    adjustment_type = "DEDUCT",
                    quantity = line.quantity,

                    adjusted_by = completedBy,
                    reference_type = "DELIVERY_CHECKLIST",

                    dr_no = dto.dr_no,
                    inv_no = dto.inv_no,
                    po_no = dto.po_no,
                    remarks = dto.remarks
                };

                await CompleteLineAsync(singleDto, completedBy);
                completedCount++;
            }

            return new
            {
                success = true,
                message = $"{completedCount} checklist line(s) completed successfully."
            };
        }
        private async Task<List<ChecklistLineCompletionData>> GetReadyChecklistLinesByCustomerAsync(
    long checklistId,
    string customerName)
        {
            var result = new List<ChecklistLineCompletionData>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
SELECT
    checklist_line_id,
    product_id,
    branch_id,
    lot_no,
    checklist_qty
FROM delivery_checklist_line
WHERE checklist_id = @checklist_id
  AND UPPER(TRIM(customer_name)) = UPPER(TRIM(@customer_name))
  AND UPPER(TRIM(status)) = 'READY'
  AND IFNULL(is_deleted, 0) = 0
ORDER BY product_name ASC, expiration_date ASC, lot_no ASC;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@checklist_id", checklistId);
            cmd.Parameters.AddWithValue("@customer_name", customerName);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new ChecklistLineCompletionData
                {
                    checklist_line_id = Convert.ToInt64(reader["checklist_line_id"]),
                    product_id = reader["product_id"]?.ToString() ?? "",
                    branch_id = reader["branch_id"]?.ToString(),
                    lot_no = reader["lot_no"]?.ToString(),
                    quantity = reader["checklist_qty"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(reader["checklist_qty"])
                });
            }

            return result;
        }
        private async Task<ChecklistLineCompletionData> GetChecklistLineForCompletionAsync(
    long checklistId,
    long checklistLineId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
SELECT
    checklist_line_id,
    product_id,
    branch_id,
    lot_no,
    checklist_qty,
    status
FROM delivery_checklist_line
WHERE checklist_id = @checklist_id
  AND checklist_line_id = @checklist_line_id
  AND IFNULL(is_deleted, 0) = 0
LIMIT 1;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@checklist_id", checklistId);
            cmd.Parameters.AddWithValue("@checklist_line_id", checklistLineId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                throw new Exception($"Checklist line not found: {checklistLineId}");

            var status = reader["status"]?.ToString() ?? "";

            if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Checklist line {checklistLineId} is not READY.");

            return new ChecklistLineCompletionData
            {
                checklist_line_id = Convert.ToInt64(reader["checklist_line_id"]),
                product_id = reader["product_id"]?.ToString() ?? "",
                branch_id = reader["branch_id"]?.ToString(),
                lot_no = reader["lot_no"]?.ToString(),
                quantity = reader["checklist_qty"] == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(reader["checklist_qty"])
            };
        }


        public async Task<object> UpdateChecklistLineLotAsync(UpdateChecklistLineLotDto dto)
        {
            if (dto.checklist_line_id <= 0)
                throw new Exception("Invalid checklist line.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("Lot No is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_id))
                throw new Exception("Warehouse is required.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
UPDATE delivery_checklist_line dcl
INNER JOIN product_lot_number pln
    ON pln.product_id = dcl.product_id
   AND pln.lot_no = @lot_no
   AND pln.branch_id = @branch_id
SET 
    dcl.lot_no = pln.lot_no,
    dcl.branch_id = pln.branch_id,
    dcl.manufacturing_date = pln.manufacturing_date,
    dcl.expiration_date = pln.expiration_date,
    dcl.updated_at = @updated_at
WHERE dcl.checklist_line_id = @checklist_line_id
  AND UPPER(TRIM(dcl.status)) = 'READY'
  AND IFNULL(dcl.is_deleted, 0) = 0
  AND IFNULL(pln.quantity, 0) >= IFNULL(dcl.checklist_qty, 0);";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@checklist_line_id", dto.checklist_line_id);
            cmd.Parameters.AddWithValue("@lot_no", dto.lot_no);
            cmd.Parameters.AddWithValue("@branch_id", dto.branch_id);
            cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

            int rows = await cmd.ExecuteNonQueryAsync();

            if (rows <= 0)
                throw new Exception("Lot not updated. Check if line is READY and selected lot has enough stock.");

            return new
            {
                success = true,
                message = "Checklist lot updated successfully."
            };
        }

        public async Task<List<object>> GetAvailableLotsForChecklistLineAsync(long checklistLineId)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            string sql = @"
SELECT
    iln.product_id,
    iln.branch_id,
    b.branch_name,
    iln.lot_no,
    iln.manufacturing_date,
    iln.expiration_date,
    iln.quantity AS on_hand_qty,

    IFNULL((
        SELECT SUM(a.allocated_qty)
        FROM daily_order_allocation a
        INNER JOIN daily_order_line ol ON ol.order_line_id = a.order_line_id
INNER JOIN daily_order_header h ON h.order_id = ol.order_id
        WHERE a.product_id = iln.product_id
          AND a.branch_id = iln.branch_id
          AND a.lot_no = iln.lot_no
          AND IFNULL(a.allocated_qty,0) > 0
          AND IFNULL(h.is_deleted,0)=0
          AND UPPER(TRIM(h.status)) NOT IN ('COMPLETED','CANCELLED')
          AND a.order_line_id <> (
              SELECT order_line_id
              FROM delivery_checklist_line
              WHERE checklist_line_id = @checklist_line_id
          )
    ),0) AS reserved_qty,

    GREATEST(
        iln.quantity -
        IFNULL((
            SELECT SUM(a.allocated_qty)
            FROM daily_order_allocation a
            INNER JOIN daily_order_line ol ON ol.order_line_id = a.order_line_id
INNER JOIN daily_order_header h ON h.order_id = ol.order_id
            WHERE a.product_id = iln.product_id
              AND a.branch_id = iln.branch_id
              AND a.lot_no = iln.lot_no
              AND IFNULL(a.allocated_qty,0)>0
              AND IFNULL(h.is_deleted,0)=0
              AND UPPER(TRIM(h.status)) NOT IN ('COMPLETED','CANCELLED')
              AND a.order_line_id <> (
                  SELECT order_line_id
                  FROM delivery_checklist_line
                  WHERE checklist_line_id = @checklist_line_id
              )
        ),0),
    0) AS available_qty

FROM product_lot_number iln
LEFT JOIN branches b ON b.branch_id = iln.branch_id
WHERE iln.product_id = (
    SELECT product_id
    FROM delivery_checklist_line
    WHERE checklist_line_id=@checklist_line_id
)
AND IFNULL(iln.quantity,0) > 0
ORDER BY iln.expiration_date, iln.manufacturing_date, iln.lot_no;";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@checklist_line_id", checklistLineId);

            var list = new List<object>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new
                {
                    product_id = reader["product_id"]?.ToString(),
                    branch_id = reader["branch_id"]?.ToString(),
                    branch_name = reader["branch_name"]?.ToString(),
                    lot_no = reader["lot_no"]?.ToString(),
                    manufacturing_date = reader["manufacturing_date"] == DBNull.Value ? null : reader["manufacturing_date"],
                    expiration_date = reader["expiration_date"] == DBNull.Value ? null : reader["expiration_date"],
                    on_hand_qty = reader["on_hand_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["on_hand_qty"]),
                    reserved_qty = reader["reserved_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["reserved_qty"]),
                    available_qty = reader["available_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["available_qty"])
                });
            }

            return list;
        }


        public async Task<object> ReplaceChecklistLotsAsync(ReplaceChecklistLotsDto dto)
        {
            if (dto.checklist_line_id <= 0)
                throw new Exception("Invalid checklist line.");

            if (dto.lots == null || dto.lots.Count == 0)
                throw new Exception("Please select at least one replacement lot.");

            var cleanedLots = dto.lots
                .Where(x => x.qty > 0)
                .ToList();

            if (cleanedLots.Count == 0)
                throw new Exception("Replacement quantity must be greater than zero.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                ChecklistLineSnapshot oldLine;

                string getOldLineSql = @"
SELECT
    checklist_id,
    order_id,
    order_no,
    order_line_id,
    customer_id,
    customer_name,
    product_id,
    product_name,
branch_id,
lot_no,
    uom,
    pack_uom,
    pack_qty,
    required_qty,
    checklist_qty,
    status
FROM delivery_checklist_line
WHERE checklist_line_id = @checklist_line_id
  AND IFNULL(is_deleted, 0) = 0
LIMIT 1;";

                using (var cmd = new MySqlCommand(getOldLineSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_line_id", dto.checklist_line_id);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new Exception("Checklist line not found.");

                    oldLine = new ChecklistLineSnapshot
                    {
                        checklist_id = Convert.ToInt64(reader["checklist_id"]),
                        order_id = Convert.ToInt64(reader["order_id"]),
                        order_no = reader["order_no"]?.ToString() ?? "",
                        order_line_id = Convert.ToInt64(reader["order_line_id"]),
                        customer_id = reader["customer_id"] == DBNull.Value ? null : reader["customer_id"]?.ToString(),
                        customer_name = reader["customer_name"] == DBNull.Value ? null : reader["customer_name"]?.ToString(),
                        product_id = reader["product_id"]?.ToString() ?? "",
                        product_name = reader["product_name"]?.ToString() ?? "",
                        branch_id = reader["branch_id"]?.ToString() ?? "",
                        lot_no = reader["lot_no"]?.ToString() ?? "",
                        uom = reader["uom"] == DBNull.Value ? null : reader["uom"]?.ToString(),
                        pack_uom = reader["pack_uom"] == DBNull.Value ? null : reader["pack_uom"]?.ToString(),
                        pack_qty = reader["pack_qty"] == DBNull.Value ? null : Convert.ToDecimal(reader["pack_qty"]),
                        required_qty = reader["required_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["required_qty"]),
                        checklist_qty = reader["checklist_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["checklist_qty"]),
                        status = reader["status"]?.ToString() ?? ""

                    };
                }

                if (!string.Equals(oldLine.status, "READY", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Only READY checklist lines can be replaced.");

                decimal totalReplacementQty = cleanedLots.Sum(x => x.qty);

                if (totalReplacementQty != oldLine.checklist_qty)
                    throw new Exception($"Replacement total must equal checklist qty. Required: {oldLine.checklist_qty}, Replacement: {totalReplacementQty}");

                string deleteOldSql = @"
UPDATE delivery_checklist_line
SET is_deleted = 1,
    remarks = @remarks,
    updated_at = @updated_at
WHERE checklist_line_id = @checklist_line_id
  AND IFNULL(is_deleted, 0) = 0;";

                using (var cmd = new MySqlCommand(deleteOldSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_line_id", dto.checklist_line_id);
                    cmd.Parameters.AddWithValue("@remarks", $"Replaced. Reason: {dto.reason ?? ""}");
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }

//                string clearOldAllocationSql = @"
//DELETE FROM daily_order_allocation
//WHERE order_line_id = @order_line_id
//  AND product_id = @product_id;";

//                using (var clearCmd = new MySqlCommand(clearOldAllocationSql, conn, transaction))
//                {
//                    clearCmd.Parameters.AddWithValue("@order_line_id", oldLine.order_line_id);
//                    clearCmd.Parameters.AddWithValue("@product_id", oldLine.product_id);
//                  //  clearCmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);

//                    await clearCmd.ExecuteNonQueryAsync();
//                }




                foreach (var lot in cleanedLots)
                {
                    string lotSql = @"
SELECT
    product_id,
    branch_id,
    lot_no,
    manufacturing_date,
    expiration_date,

    GREATEST(
        quantity -
        IFNULL((
            SELECT SUM(a.allocated_qty)
            FROM daily_order_allocation a
            INNER JOIN daily_order_line ol
                ON ol.order_line_id = a.order_line_id
            INNER JOIN daily_order_header h
                ON h.order_id = ol.order_id
            WHERE a.product_id = product_lot_number.product_id
              AND a.branch_id = product_lot_number.branch_id
              AND a.lot_no = product_lot_number.lot_no
              AND IFNULL(a.allocated_qty,0) > 0
              AND IFNULL(h.is_deleted,0)=0
              AND UPPER(TRIM(h.status)) NOT IN ('COMPLETED','CANCELLED')
AND a.order_line_id <> @order_line_id
        ),0),
    0) AS available_qty

FROM product_lot_number

WHERE product_id=@product_id
AND branch_id=@branch_id
AND lot_no=@lot_no

HAVING available_qty>=@qty
LIMIT 1;";

                    AllocationLotItem replacementLot;




                    using (var cmd = new MySqlCommand(lotSql, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@product_id", oldLine.product_id);
                        cmd.Parameters.AddWithValue("@branch_id", lot.branch_id);
                        cmd.Parameters.AddWithValue("@lot_no", lot.lot_no);
                        cmd.Parameters.AddWithValue("@qty", lot.qty);
                        cmd.Parameters.AddWithValue("@order_line_id", oldLine.order_line_id);

                        using var reader = await cmd.ExecuteReaderAsync();

                        if (!await reader.ReadAsync())
                            throw new Exception($"Lot {lot.lot_no} does not have enough stock.");

                        replacementLot = new AllocationLotItem
                        {
                            branch_id = reader["branch_id"]?.ToString(),
                            lot_no = reader["lot_no"]?.ToString() ?? "",
                            manufacturing_date = reader["manufacturing_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["manufacturing_date"]),
                            expiration_date = reader["expiration_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["expiration_date"]),
                            allocated_qty = lot.qty
                        };
                    }

                    var newLine = new ChecklistLineDto
                    {
                        order_id = oldLine.order_id,
                        order_no = oldLine.order_no,
                        order_line_id = oldLine.order_line_id,
                        customer_id = oldLine.customer_id,
                        customer_name = oldLine.customer_name,
                        product_id = oldLine.product_id,
                        product_name = oldLine.product_name,
                        uom = oldLine.uom,
                        pack_uom = oldLine.pack_uom,
                        pack_qty = oldLine.pack_qty,
                        required_qty = oldLine.required_qty,
                        allocated_qty = lot.qty,
                        checklist_qty = lot.qty
                    };

                    string updateAllocationSql = @"
UPDATE daily_order_allocation
SET
    branch_id = @new_branch_id,
    lot_no = @new_lot_no,
    manufacturing_date = @manufacturing_date,
    expiration_date = @expiration_date,
    allocated_qty = @allocated_qty
WHERE order_line_id = @order_line_id
  AND product_id = @product_id
  AND branch_id = @old_branch_id
  AND lot_no = @old_lot_no;";

                    using (var allocCmd = new MySqlCommand(updateAllocationSql, conn, transaction))
                    {
                        allocCmd.Parameters.AddWithValue("@order_line_id", oldLine.order_line_id);
                        allocCmd.Parameters.AddWithValue("@product_id", oldLine.product_id);

                        allocCmd.Parameters.AddWithValue("@old_branch_id", oldLine.branch_id);
                        allocCmd.Parameters.AddWithValue("@old_lot_no", oldLine.lot_no);

                        allocCmd.Parameters.AddWithValue("@new_branch_id", lot.branch_id);
                        allocCmd.Parameters.AddWithValue("@new_lot_no", lot.lot_no);
                        allocCmd.Parameters.AddWithValue("@manufacturing_date", (object?)replacementLot.manufacturing_date ?? DBNull.Value);
                        allocCmd.Parameters.AddWithValue("@expiration_date", (object?)replacementLot.expiration_date ?? DBNull.Value);
                        allocCmd.Parameters.AddWithValue("@allocated_qty", lot.qty);

                        int allocRows = await allocCmd.ExecuteNonQueryAsync();

                        if (allocRows <= 0)
                            throw new Exception("Allocation row was not updated. Old lot allocation not found.");
                    }

                    await InsertChecklistLinePerLotAsync(
                        conn,
                        transaction,
                        oldLine.checklist_id,
                        newLine,
                        replacementLot
                    );

                   
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Checklist lot replacement saved."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<object> DeleteChecklistLineAsync(long checklistLineId)
        {
            if (checklistLineId <= 0)
                throw new Exception("Invalid checklist line.");

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                long checklistId = 0;
                string status = "";

                string checkSql = @"
SELECT checklist_id, status
FROM delivery_checklist_line
WHERE checklist_line_id = @checklist_line_id
  AND IFNULL(is_deleted, 0) = 0
LIMIT 1;";

                using (var cmd = new MySqlCommand(checkSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_line_id", checklistLineId);

                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new Exception("Checklist line not found.");

                    checklistId = Convert.ToInt64(reader["checklist_id"]);
                    status = reader["status"]?.ToString() ?? "";
                }

                if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "LOADING", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Only READY or LOADING checklist line can be deleted.");
                }

                string deleteSql = @"
UPDATE delivery_checklist_line
SET is_deleted = 1,
    updated_at = @updated_at
WHERE checklist_line_id = @checklist_line_id;";

                using (var cmd = new MySqlCommand(deleteSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_line_id", checklistLineId);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }

                string countSql = @"
SELECT COUNT(*)
FROM delivery_checklist_line
WHERE checklist_id = @checklist_id
  AND IFNULL(is_deleted, 0) = 0;";

                long remainingLines;

                using (var cmd = new MySqlCommand(countSql, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    remainingLines = Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }

                if (remainingLines == 0)
                {
                    string deleteHeaderSql = @"
UPDATE delivery_checklist_header
SET is_deleted = 1,
    updated_at = @updated_at
WHERE checklist_id = @checklist_id;";

                    using var cmd = new MySqlCommand(deleteHeaderSql, conn, transaction);
                    cmd.Parameters.AddWithValue("@checklist_id", checklistId);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return new
                {
                    success = true,
                    message = "Checklist line deleted successfully."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }



    }


}