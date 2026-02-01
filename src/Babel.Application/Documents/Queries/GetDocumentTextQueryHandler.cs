using Babel.Application.Common;
using Babel.Application.Interfaces;

namespace Babel.Application.Documents.Queries;

public sealed class GetDocumentTextQueryHandler : IQueryHandler<GetDocumentTextQuery, string?>
{
    private readonly IDocumentRepository _documentRepository;

    public GetDocumentTextQueryHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<Result<string?>> Handle(
        GetDocumentTextQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure<string?>(DomainErrors.Document.NotFound);
        }

        return Result.Success<string?>(document.Content);
    }
}
