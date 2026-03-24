using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace inventory_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FirestoreTestController : ControllerBase
    {
        private readonly FirestoreDb _firestoreDb;

        public FirestoreTestController(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var snapshot = await _firestoreDb.Collection("test_connection").GetSnapshotAsync();

            return Ok(new
            {
                message = "Connected to Firestore successfully",
                count = snapshot.Count
            });
        }
    }
}
