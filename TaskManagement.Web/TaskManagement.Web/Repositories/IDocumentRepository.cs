using TaskManagement.Web.Models;

namespace TaskManagement.Web.Repositories
{
    public interface IDocumentRepository
    {
        // Lấy tất cả tài liệu (bản mới nhất của mỗi nhóm)
        IEnumerable<Document> GetAll();

        // Lấy tất cả phiên bản của một nhóm tài liệu
        IEnumerable<Document> GetVersions(int groupId);

        // Lấy một tài liệu theo id
        Document GetById(int documentId);

        // Lấy phiên bản mới nhất của một nhóm
        Document GetLatestByGroupId(int groupId);

        // Thêm tài liệu mới
        void Add(Document document);

        // Xóa tài liệu
        void Delete(int documentId);

        // Lấy GroupId lớn nhất hiện tại (để tạo group mới)
        int GetMaxGroupId();

        void Save();
    }
}