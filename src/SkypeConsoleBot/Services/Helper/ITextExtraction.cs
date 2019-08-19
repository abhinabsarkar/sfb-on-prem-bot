using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.Helper
{
    interface ITextExtraction
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task<string> GetMessageFromHref(string hrefString);
        Task<string> GetMessageFromHtml(string hrefString);
    }
}
