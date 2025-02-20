
using Defra.PTS.Common.Repositories.Interface;
using System.Diagnostics.CodeAnalysis;

namespace Defra.PTS.Common.ApiServices.Configuration
{
    [ExcludeFromCodeCoverage]
    public class RepositoryConfiguration
    {
        public required IApplicationRepository ApplicationRepository { get; set; }
        public required IOwnerRepository OwnerRepository { get; set; }
        public required IAddressRepository AddressRepository { get; set; }
        public required IPetRepository PetRepository { get; set; }
        public required IUserRepository UserRepository { get; set; }
        public required ITravelDocumentRepository TravelDocumentRepository { get; set; }
        public required IBreedRepository BreedRepository { get; set; }
    }
}