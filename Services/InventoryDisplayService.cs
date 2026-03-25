using Google.Cloud.Firestore;
using inventory_api.DTOs;

namespace inventory_api.Services
{
    public class InventoryDisplayService
    {
        private readonly FirestoreDb _firestoreDb;

        public InventoryDisplayService(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<List<InventoryDisplayDto>> GetAllAsync()
        {
            var lotSnapshot = await _firestoreDb.Collection("product_lot_number").GetSnapshotAsync();
            var productSnapshot = await _firestoreDb.Collection("products").GetSnapshotAsync();
            var branchSnapshot = await _firestoreDb.Collection("branches").GetSnapshotAsync();

            var productDict = productSnapshot.Documents
                .Select(d => d.ToDictionary())
                .ToDictionary(x => x["product_id"].ToString(), x => x);

            var branchDict = branchSnapshot.Documents
                .Select(d => d.ToDictionary())
                .ToDictionary(x => x["branch_id"].ToString(), x => x);

            var result = new List<InventoryDisplayDto>();

            foreach (var doc in lotSnapshot.Documents)
            {
                var lot = doc.ToDictionary();

                var productId = lot["product_id"].ToString();
                var branchId = lot["branch_id"].ToString();

                productDict.TryGetValue(productId, out var product);
                branchDict.TryGetValue(branchId, out var branch);

                int qty = 0;
                int.TryParse(lot["quantity"]?.ToString(), out qty);

                result.Add(new InventoryDisplayDto
                {
                    product_id = productId,
                    description = product != null && product.ContainsKey("product_description")
                        ? product["product_description"].ToString()
                        : "",
                    uom = product != null && product.ContainsKey("uom")
                        ? product["uom"].ToString()
                        : "",
                    lot_no = lot["lot_no"]?.ToString(),
                    warehouse = branch != null && branch.ContainsKey("branch_name")
                        ? branch["branch_name"].ToString()
                        : branchId,
                    qty = qty,
                    date = lot.ContainsKey("manufacturing_date")
                        ? lot["manufacturing_date"].ToString()
                        : ""
                });
            }

            return result;
        }
    }
}