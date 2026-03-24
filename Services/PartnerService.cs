using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class PartnerService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "partners";

        public PartnerService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<Dictionary<string, object>> partners = new();

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
                    partners.Add(data);
                    }
                
            }

            return partners;
        }

        //public async Task<Dictionary<string, object>?> GetByBarcodeAsync(string product_sku)
        //{
        //    Query query = _firestoreDb.Collection(_collectionName)
        //                              .WhereEqualTo("product_sku", product_sku)
        //                              .Limit(1);

        //    QuerySnapshot snapshot = await query.GetSnapshotAsync();
        //    DocumentSnapshot? doc = snapshot.Documents.FirstOrDefault();

        //    if (doc == null || !doc.Exists)
        //        return null;

        //    var data = doc.ToDictionary();
        //    data["doc_id"] = doc.Id;
        //    return data;
        //}

        public async Task AddAsync(CreatePartnerDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "partners_id", dto.partner_id },
                { "partners_name", dto.partner_name },
                { "address", dto.address },
                 { "contact_no", dto.contact_no },
                 { "partner_type", dto.partner_type },
                { "created_at", Timestamp.GetCurrentTimestamp() },
                 { "updated_at", Timestamp.GetCurrentTimestamp() }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.partner_id)
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