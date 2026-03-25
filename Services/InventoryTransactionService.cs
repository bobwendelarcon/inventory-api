using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class InventoryTransactionService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _transactionCollection = "inventory_transactions";
        private readonly string _lotCollection = "product_lot_number";

        public InventoryTransactionService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task AddAsync(CreateInventoryTransactionDto dto)
        {
            if (dto == null)
                throw new Exception("Invalid request.");

            //if (string.IsNullOrWhiteSpace(dto.transaction_id))
            //    throw new Exception("transaction_id is required.");

            if (string.IsNullOrWhiteSpace(dto.product_id))
                throw new Exception("product_id is required.");

            if (string.IsNullOrWhiteSpace(dto.branch_id))
                throw new Exception("branch_id is required.");

            if (string.IsNullOrWhiteSpace(dto.lot_no))
                throw new Exception("lot_no is required.");

            if (string.IsNullOrWhiteSpace(dto.transaction_type))
                throw new Exception("transaction_type is required.");

            if (dto.quantity <= 0)
                throw new Exception("quantity must be greater than 0.");

            string transactionType = dto.transaction_type.Trim().ToUpper();

            if (transactionType != "IN" && transactionType != "OUT")
                throw new Exception("transaction_type must be IN or OUT.");

            string lotDocId = $"{dto.product_id}_{dto.branch_id}_{dto.lot_no}";

            //DocumentReference transactionRef = _firestoreDb
            //    .Collection(_transactionCollection)
            //    .Document(dto.transaction_id);

            //auto
            DocumentReference transactionRef = _firestoreDb
    .Collection(_transactionCollection)
    .Document(); // auto ID

            DocumentReference lotRef = _firestoreDb
                .Collection(_lotCollection)
                .Document(lotDocId);

            await _firestoreDb.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot lotSnapshot = await transaction.GetSnapshotAsync(lotRef);

                double existingQty = 0;

                if (lotSnapshot.Exists && lotSnapshot.ContainsField("quantity"))
                {
                    object qtyObj = lotSnapshot.GetValue<object>("quantity");

                    if (qtyObj is long longQty)
                        existingQty = longQty;
                    else if (qtyObj is int intQty)
                        existingQty = intQty;
                    else if (qtyObj is double doubleQty)
                        existingQty = doubleQty;
                    else if (qtyObj is string stringQty && double.TryParse(stringQty, out double parsedQty))
                        existingQty = parsedQty;
                }

                if (transactionType == "IN")
                {
                    double newQty = existingQty + dto.quantity;

                    var lotData = new Dictionary<string, object>
                    {
                        { "lot_no", dto.lot_no },
                        { "product_id", dto.product_id },
                        { "branch_id", dto.branch_id },
                        { "quantity", newQty },
                        { "updated_at", Timestamp.GetCurrentTimestamp() }
                    };

                    if (!lotSnapshot.Exists)
                    {
                        lotData["created_at"] = Timestamp.GetCurrentTimestamp();
                    }

                    transaction.Set(lotRef, lotData, SetOptions.MergeAll);
                }
                else if (transactionType == "OUT")
                {
                    if (!lotSnapshot.Exists)
                        throw new Exception("Lot not found.");

                    if (existingQty < dto.quantity)
                        throw new Exception("Insufficient stock.");

                    double newQty = existingQty - dto.quantity;

                    transaction.Update(lotRef, new Dictionary<string, object>
                    {
                        { "quantity", newQty },
                        { "updated_at", Timestamp.GetCurrentTimestamp() }
                    });
                }

                var transactionData = new Dictionary<string, object>
                {
                    { "transaction_id", transactionRef.Id },
                    { "product_id", dto.product_id },
                    { "branch_id", dto.branch_id },
                    { "transaction_type", transactionType },
                    { "lot_no", dto.lot_no },
                    { "quantity", dto.quantity },
                    { "scanned_by", dto.scanned_by },
                    { "remarks", dto.remarks },
                    { "partner", dto.partner },
                    { "timestamp", Timestamp.GetCurrentTimestamp() },
                    { "is_deleted", false }
                };

                transaction.Set(transactionRef, transactionData);
            });
        }
        public async Task ClearAllDataAsync()
        {
            await DeleteCollectionAsync("inventory_transactions");
            await DeleteCollectionAsync("product_lot_number");
        }

        private async Task DeleteCollectionAsync(string collectionName)
        {
            var collection = _firestoreDb.Collection(collectionName);
            var snapshot = await collection.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
        }
        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_transactionCollection).GetSnapshotAsync();

            List<Dictionary<string, object>> result = new();

            //foreach (var doc in snapshot.Documents)
            //{
            //    if (doc.Exists)
            //    {
            //        var data = doc.ToDictionary();

            //        if (data.ContainsKey("timestamp") && data["timestamp"] is Timestamp timestamp)
            //        {
            //            data["timestamp"] = timestamp.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            //        }

            //        if (data.ContainsKey("created_at") && data["created_at"] is Timestamp createdAt)
            //        {
            //            data["created_at"] = createdAt.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            //        }

            //        if (data.ContainsKey("updated_at") && data["updated_at"] is Timestamp updatedAt)
            //        {
            //            data["updated_at"] = updatedAt.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
            //        }

            //        data["doc_id"] = doc.Id;
            //        result.Add(data);
            //    }
            //}
            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();

                    if (data.ContainsKey("timestamp") && data["timestamp"] is Timestamp timestamp)
                    {
                        var utcDate = timestamp.ToDateTime();

                        TimeZoneInfo phTimeZone;

                        try
                        {
                            phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
                        }
                        catch
                        {
                            phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
                        }

                        var phDate = TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(utcDate, DateTimeKind.Utc),
                            phTimeZone
                        );

                        data["timestamp"] = phDate.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    data["doc_id"] = doc.Id;
                    result.Add(data);
                }
            }

            return result;
        }
    }
}