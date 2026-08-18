using AutoMapper;
using CPAWeb.Data.Model;
using CPAWeb.Services.DTOs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CPAWeb.Services.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<SID, SIDDto>();

            CreateMap<SIDDto, SID>();

            CreateMap<CreateSIDDto, SID>();

            CreateMap<SIDSearchResult, SIDSearchResultDto>();
        }
    }
}