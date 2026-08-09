using MediatR;
using Recruitment.API.DTOs;

namespace Recruitment.API.Features.Commands.UpdateSelectionRounds
{
    public class UpdateSelectionRoundsCommand : IRequest<bool>
    {
        public Guid ProcessId { get; set; }
        public List<SelectionRoundInsertDto> Rounds { get; set; } = [];
    }
}
