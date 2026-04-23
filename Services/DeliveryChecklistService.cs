using inventory_api.DTOs;
using MySqlConnector;
using System.Data;

namespace inventory_api.Services
{
    public class DeliveryChecklistService
    {
        private readonly string _connectionString;

        public DeliveryChecklistService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new Exception("Connection string not found.");
        }
        private class AllocationLotItem
        {
            public string lot_no { get; set; } = string.Empty;
            public string? branch_id { get; set; }   // ✅ NEW
            public DateTime? manufacturing_date { get; set; }
            public DateTime? expiration_date { get; set; }
            public decimal allocated_qty { get; set; }
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

                long checklistId = await InsertChecklistHeaderAsync(conn, transaction, dto, checklistNo, createdBy);

                foreach (var line in dto.lines)
                {
                    await ValidateChecklistLineAsync(conn, transaction, line);

                    var allocationLots = await GetAllocationLotsAsync(conn, transaction, line.order_line_id);

                    if (allocationLots.Count == 0)
                        throw new Exception($"No allocated lots found for order line {line.order_line_id}.");

                    decimal totalLotAllocatedQty = allocationLots.Sum(x => x.allocated_qty);

                    if (totalLotAllocatedQty <= 0)
                        throw new Exception($"Allocated lot qty is zero for order line {line.order_line_id}.");
                    var distinctBranches = allocationLots
    .Where(x => !string.IsNullOrWhiteSpace(x.branch_id))
    .Select(x => x.branch_id)
    .Distinct()
    .ToList();

                    if (distinctBranches.Count > 1)
                        throw new Exception($"Mixed-branch allocation detected for order line {line.order_line_id}. This is not allowed.");

                    foreach (var lot in allocationLots)
                    {
                        await InsertChecklistLinePerLotAsync(conn, transaction, checklistId, line, lot);
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
    p.uom,
    p.pack_uom,
    p.pack_qty,
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
    AND ol.remaining_qty > 0
    AND UPPER(TRIM(oh.status)) IN ('READY FOR DISPATCH', 'PARTIAL', 'PARTIALLY DELIVERED')
    AND NOT EXISTS
(
    SELECT 1
    FROM delivery_checklist_line dcl
    INNER JOIN delivery_checklist_header dch
        ON dcl.checklist_id = dch.checklist_id
    WHERE 
        dcl.order_id = ol.order_id
        AND dcl.product_id = ol.product_id
        AND IFNULL(dcl.is_deleted, 0) = 0
        AND IFNULL(dch.is_deleted, 0) = 0
        AND UPPER(TRIM(dch.status)) IN ('READY', 'LOADING', 'PARTIAL')
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
                    uom = reader["uom"] == DBNull.Value ? null : reader["uom"]?.ToString(),
                    pack_uom = reader["pack_uom"] == DBNull.Value ? null : reader["pack_uom"]?.ToString(),
                    pack_qty = reader["pack_qty"] == DBNull.Value ? null : Convert.ToDecimal(reader["pack_qty"]),
                    required_qty = reader["required_qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["required_qty"]),
                    allocated_qty = allocatedQty,
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
    checklist_line_id,
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
    

    manufacturing_date,
    expiration_date,
    required_qty,
    allocated_qty,
    checklist_qty,
    released_qty,
    remaining_qty,
    status,
    remarks
FROM delivery_checklist_line
WHERE checklist_id = @checklist_id
  AND is_deleted = 0
ORDER BY product_name ASC, expiration_date ASC, lot_no ASC;";

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
                        branch_id = reader["branch_id"] == DBNull.Value ? null : reader["branch_id"]?.ToString(),
                        lot_no = reader["lot_no"] == DBNull.Value ? null : reader["lot_no"]?.ToString(),
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

                if (!string.Equals(status, "READY", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Only READY checklist can be deleted.");
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




    }


}