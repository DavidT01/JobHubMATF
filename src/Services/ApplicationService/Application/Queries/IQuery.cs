using MediatR;

namespace ApplicationService.Application.Queries;

public interface IQuery<out TResponse> : IRequest<TResponse>;
