using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class BranchesService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _collectionName = "branches";

        public BranchesService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<Dictionary<string, object>>> GetAllAsync()
        {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<Dictionary<string, object>> branches = new();

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
                    branches.Add(data);
                }
            }

            return branches;
        }

       

        public async Task AddAsync(CreateBranchesDto dto)
        {
            var data = new Dictionary<string, object>
            {
                { "branch_id", dto.branch_id },
                { "branch_name", dto.branch_name },
                { "branch_loc", dto.branch_loc },
               
                { "created_at", Timestamp.GetCurrentTimestamp() },
                 { "updated_at", Timestamp.GetCurrentTimestamp() }
            };

            await _firestoreDb.Collection(_collectionName)
                              .Document(dto.branch_id)
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