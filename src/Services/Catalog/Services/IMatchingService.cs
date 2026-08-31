using Catalog.DTOs;
using Catalog.Entities;

namespace Catalog.Services;

public interface IMatchingService
{
    MatchResultDto CalculateMatch(Job job , CandidateProfileDto candidate);
}