using Babel.Application.Common;

namespace Babel.Application.Documents.Queries;

public sealed record GetDocumentTextQuery(Guid Id) : IQuery<string?>;
