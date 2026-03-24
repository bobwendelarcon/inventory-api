using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class CategoryService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "categories";

        public CategoryService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<Dictionary<string, object>> categories = new();

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
                    categories.Add(data);
                }
            }

            return categories;
        }

        public async Task AddAsync(CreateCategoryDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "catg_id", dto.catg_id },
                { "catg_name", dto.catg_name },
                { "catg_desc", dto.catg_desc },
                { "created_at", Timestamp.GetCurrentTimestamp() },
                 { "updated_at", Timestamp.GetCurrentTimestamp() }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.catg_id)
                              .SetAsync(data);
   
        }
    }
}