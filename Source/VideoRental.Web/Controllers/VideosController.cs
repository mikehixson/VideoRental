using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using VideoRental.Core;

namespace VideoRental.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VideosController : ControllerBase
    {
        private readonly IVideoRepository _videoRepository;

        public VideosController(IVideoRepository videoRepository)
        {
            _videoRepository = videoRepository;
        }

        [HttpGet]
        public IEnumerable<Video> Get(int category = 1868, int page = 0, int sort = 3, int show = 50)
        {
            return _videoRepository.GetAll(category, page, sort, show).ToArray();   //todo: Async
        }
    }
}
