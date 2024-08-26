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

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] Poll request)
    {
        var poll = _pollService.Update(id, request);

        return poll is false ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var poll = _pollService.Delete(id);

        return poll is false ? NotFound() : NoContent();
    }
}
