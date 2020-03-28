using AutoMapper;
using CommunicationApi.Contracts.v1;

namespace CommunicationApi
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<BoxStatus, Models.BoxStatus>();
            CreateMap<ActivatedDevice, Models.ActivatedDevice>();
            CreateMap<Models.BoxStatus, BoxStatus>();
            CreateMap<Models.ActivatedDevice, ActivatedDevice>();
        }
    }
}