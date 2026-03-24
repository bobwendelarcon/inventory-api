using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class ProductService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "products";

        public ProductService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<Dictionary<string, object>> products = new();

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
                        products.Add(data);
                    }
                
            }

            return products;
        }

        public async Task<Dictionary<string, object>?> GetByBarcodeAsync(string product_sku)
        {
            Query query = _firestoreDb.Collection(_collectionName)
                                      .WhereEqualTo("product_sku", product_sku)
                                      .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            DocumentSnapshot? doc = snapshot.Documents.FirstOrDefault();

            if (doc == null || !doc.Exists)
                return null;

            var data = doc.ToDictionary();
            data["doc_id"] = doc.Id;
            return data;
        }

        public async Task AddAsync(CreateProductDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "product_id", dto.product_id },
                { "product_sku", dto.product_sku },
                { "product_name", dto.product_name },
                  { "product_description", dto.product_name },
                      { "product_price", dto.product_price },
                { "uom", dto.uom },
                 { "stock_level", dto.stock_level },
                { "catg_id", dto.catg_id },
               
            
                { "is_deleted", 0 },
                { "created_at", Timestamp.GetCurrentTimestamp() },
                 { "updated_at", Timestamp.GetCurrentTimestamp() }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.product_id)
                              .SetAsync(data);
  
        }
        public async Task<bool> SoftDeleteAsync(string productId)
        {
            var docRef = _firestoreDb.Collection(_collectionName).Document(productId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return false;

            var updates = new Dictionary<string, object>
    {
        { "is_deleted", true }
    };

            await docRef.UpdateAsync(updates);
            return true;
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