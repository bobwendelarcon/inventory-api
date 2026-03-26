using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class ProductLotNumberService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "product_lot_number";

        public ProductLotNumberService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<Dictionary<string, object>> product_lot_number = new();

            foreach (DocumentSnapshot doc in snapshot.Documents)
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
                    product_lot_number.Add(data);
                    }
                
            }

            return product_lot_number;
        }

       

        public async Task AddAsync(CreateProductLotNumberDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "lot_no", dto.lot_no },
                { "product_id", dto.product_id },
                { "manufacturing_date", dto.manufacturing_date },
                 { "expiration_date", dto.expiration_date },
                 { "quantity", dto.quantity },
                 { "branch_id", dto.branch_id },
            
                { "created_at", Timestamp.GetCurrentTimestamp() },
                 { "updated_at", Timestamp.GetCurrentTimestamp() }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.product_id)
                              .SetAsync(data);
  
        }
        //    public async Task<bool> SoftDeleteAsync(string productId)
        //    {
        //        var docRef = _firestoreDb.Collection(_collectionName).Document(productId);
        //        var snapshot = await docRef.GetSnapshotAsync();

        //        if (!snapshot.Exists)
        //            return false;

        //        var updates = new Dictionary<string, object>
        //{
        //    { "is_deleted", true }
        //};

        //        await docRef.UpdateAsync(updates);
        //        return true;
        //    }

        public async Task<Dictionary<string, object>?> GetByLotNoAsync(string lot_no)
        {
            Query query = _firestoreDb.Collection(_collectionName)
                                      .WhereEqualTo("lot_no", lot_no)
                                      .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            DocumentSnapshot? doc = snapshot.Documents.FirstOrDefault();

            if (doc == null || !doc.Exists)
                return null;

            var data = doc.ToDictionary();
            data["doc_id"] = doc.Id;
            return data;
        }

        public async Task<List<Dictionary<string, object>>> GetByProductIDAsync(string product_id)
        {
            Query query = _firestoreDb.Collection(_collectionName)
                                      .WhereEqualTo("product_id", product_id);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            List<Dictionary<string, object>> result = new();

            foreach (var doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    var data = doc.ToDictionary();
                    data["doc_id"] = doc.Id;

                    // optional: convert timestamp (same as your other method)
                    if (data.ContainsKey("timestamp") && data["timestamp"] is Timestamp timestamp)
                    {
                        data["timestamp"] = timestamp.ToDateTime().ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    result.Add(data);
                }
            }

            return result;
        }


        public async Task ResetAllAsync()
        {
            var snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
        }
    }
}