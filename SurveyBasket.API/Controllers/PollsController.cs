namespace SurveyBasket.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PollsController(IPollSerivce pollSerivce) : ControllerBase
{
   private readonly IPollSerivce _pollService = pollSerivce;

    [HttpGet("")]
    public IActionResult GetAll()
    {
        return Ok(_pollService.GetAll());
    }

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var poll = _pollService.Get(id);

        return poll is null ? NotFound(): Ok(poll);
    }

    [HttpPost("")]
    public IActionResult Create([FromBody] Poll request)
    {
        var newPoll = _pollService.Create(request);
        return CreatedAtAction(nameof(Get), new {id = newPoll.Id}, newPoll);
    }
}
