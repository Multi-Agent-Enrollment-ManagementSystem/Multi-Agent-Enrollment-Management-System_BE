using System.Text.RegularExpressions;

namespace MAEMS.MultiAgent.Agents.ChatBoxAgent;

/// <summary>
/// Classifies user queries to determine if they require relationship-aware processing
/// </summary>
public class QueryClassifier
{
    private static readonly string[] RelationshipKeywords = new[]
    {
        // Vietnamese relationship keywords
        "có những", "những gì", "liệt kê", "danh sách",
        "bao nhiêu ngành", "bao nhiêu chương trình",
        "phương thức nào", "campus nào", "cơ sở nào",

        // Relationship patterns
        "ngành.*phương thức", "chương trình.*campus",
        "major.*admission", "program.*type"
    };

    private static readonly string[] SimpleInfoKeywords = new[]
    {
        "là gì", "giới thiệu", "mô tả",
        "thông tin liên hệ", "địa chỉ", "hotline",
        "học phí", "thời gian học"
    };

    public QueryType ClassifyQuery(string query)
    {
        var lowerQuery = query.ToLower();

        // Check if query requires relationship understanding
        foreach (var keyword in RelationshipKeywords)
        {
            if (lowerQuery.Contains(keyword) || Regex.IsMatch(lowerQuery, keyword))
            {
                return QueryType.RelationshipBased;
            }
        }

        // Check if it's a simple information query
        foreach (var keyword in SimpleInfoKeywords)
        {
            if (lowerQuery.Contains(keyword))
            {
                return QueryType.SimpleInformation;
            }
        }

        // Default to RAG-only
        return QueryType.RagOnly;
    }

    public bool RequiresSqlJoin(string query)
    {
        return ClassifyQuery(query) == QueryType.RelationshipBased;
    }
}

public enum QueryType
{
    /// <summary>
    /// Simple query that can be answered with RAG only (e.g., "CNTT là gì?")
    /// </summary>
    RagOnly,

    /// <summary>
    /// Query requiring relationship understanding (e.g., "CNTT có những phương thức nào?")
    /// </summary>
    RelationshipBased,

    /// <summary>
    /// Simple information lookup (contact info, description)
    /// </summary>
    SimpleInformation
}
