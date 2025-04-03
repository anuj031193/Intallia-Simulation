//using System.Collections.Generic;
//using System.Threading.Tasks;
//using JobSimulation.DAL;
//using JobSimulation.Models;

//namespace JobSimulation.BLL
//{
//    public class SimulationService
//    {
//        private readonly FileService _fileService;
//        private readonly ValidationService _validationService;
//        private readonly SectionRepository _sectionRepository;
//        private readonly SimulationRepository _simulationRepository;

//        public SimulationService(FileService fileService, ValidationService validationService, SectionRepository sectionRepository, SimulationRepository simulationRepository)
//        {
//            _fileService = fileService;
//            _validationService = validationService;
//            _sectionRepository = sectionRepository;
//            _simulationRepository = simulationRepository;
//        }

//        public async Task<IEnumerable<Simulation>> GetAllSimulationsAsync()
//        {
//            return await _simulationRepository.GetAllSimulationsAsync();
//        }
//    }
//}