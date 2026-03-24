using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class InventoryTransactionService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "inventory_transactions";

        public InventoryTransactionService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task AddAsync(CreateInventoryTransactionDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "transaction_id", dto.transaction_id },
                { "product_id", dto.product_id },
                { "branch_id", dto.branch_id },
                { "transaction_type", dto.transaction_type },
                { "lot_no", dto.lot_no },
                { "quantity", dto.quantity },
                { "scanned_by", dto.scanned_by },
                { "remarks", dto.remarks },
                { "partner", dto.partner },

                // controlled by backend
                { "timestamp", Timestamp.GetCurrentTimestamp() },
                { "is_deleted", false }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.transaction_id)
                              .SetAsync(data);
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();

            List<Dictionary<string, object>> result = new();

            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();

                    if (data.ContainsKey("created_at") && data["created_at"] is Timestamp createdAt)
                    {
                        data["created_at"] = createdAt.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    if (data.ContainsKey("updated_at") && data["updated_at"] is Timestamp updatedAt)
                    {
                        data["updated_at"] = updatedAt.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    data["doc_id"] = doc.Id;
                    result.Add(data);
                }
            }

            return result;
        }
    }
}