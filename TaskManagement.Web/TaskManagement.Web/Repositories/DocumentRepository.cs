using TaskManagement.Web.Data;
using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        // Lấy bản mới nhất của mỗi nhóm
        public IEnumerable<Document> GetAll()
        {
            // Load hết về memory trước rồi mới GroupBy
            var all = _context.Documents.ToList();

            return all
                .GroupBy(d => d.GroupId ?? d.DocumentId)
                .Select(g => g.OrderByDescending(d => d.Version).First())
                .OrderByDescending(d => d.UploadedAt)
                .ToList();
        }

        public IEnumerable<Document> GetVersions(int groupId)
        {
            return _context.Documents
                .Where(d => d.GroupId == groupId || d.DocumentId == groupId)
                .OrderByDescending(d => d.Version)
                .ToList();
        }

        public Document GetById(int documentId)
        {
            return _context.Documents.Find(documentId);
        }

        public Document GetLatestByGroupId(int groupId)
        {
            return _context.Documents
                .Where(d => d.GroupId == groupId)
                .OrderByDescending(d => d.Version)
                .FirstOrDefault();
        }

        public void Add(Document document)
        {
            _context.Documents.Add(document);
        }

        public void Delete(int documentId)
        {
            var doc = _context.Documents.Find(documentId);
            if (doc != null)
                _context.Documents.Remove(doc);
        }

        public int GetMaxGroupId()
        {
            if (!_context.Documents.Any()) return 0;
            return _context.Documents.Max(d => d.GroupId ?? d.DocumentId);
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}