using System;
using System.Threading.Tasks;
using AutoMapper;
using CPAWeb.Services.DTOs;
using CPAWeb.Data.Interface;
using CPAWeb.Services.DTOs;
using CPAWeb.Services.Interface;
using CPAWeb.Data.Model;

namespace CPAWeb.Business.Services.Services
{
    public class SIDService : ISIDService
    {
        private readonly ISIDRepository _sidRepository;
        private readonly IMapper _mapper;

        public SIDService(ISIDRepository sidRepository, IMapper mapper)
        {
            _sidRepository = sidRepository ?? throw new ArgumentNullException(nameof(sidRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<bool> AddSIDAsync(CreateSIDDto createDto)
        {
            var entity = _mapper.Map<SID>(createDto);
            return await _sidRepository.AddSIDAsync(entity);
        }

        public async Task<SIDDto?> GetSIDByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty.", nameof(name));
            }

            var entity = await _sidRepository.GetSIDByNameAsync(name);

            if (entity == null)
            {
                return null;
            }

            return _mapper.Map<SIDDto>(entity);
        }
    }
}