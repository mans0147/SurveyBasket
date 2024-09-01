namespace SurveyBasket.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PollsController(IPollSerivce pollSerivce) : ControllerBase
{
   private readonly IPollSerivce _pollService = pollSerivce;

    [HttpGet("")]
    public IActionResult GetAll()
    {
        var polls = _pollService.GetAll();
        var response = polls.Adapt<IEnumerable<PollResponse>>();

        return Ok(response);
    }

    [HttpGet("{id}")]
    public IActionResult Get([FromRoute] int id)
    {
        var poll = _pollService.Get(id);

        if (poll is null)
            return NotFound();

        var response = poll.Adapt<PollResponse>();

        return Ok(response);
    }

    [HttpPost("")]
    public IActionResult Create([FromBody] CreatePollRequest request)
    {
        var newPoll = _pollService.Create(request.Adapt<Poll>());
        return CreatedAtAction(nameof(Get), new {id = newPoll.Id}, newPoll);
    }

    [HttpPut("{id}")]
    public IActionResult Update([FromRoute] int id, [FromBody] CreatePollRequest request)
    {
        var isUpdated = _pollService.Update(id, request.Adapt<Poll>());

        if(!isUpdated)
            return NotFound();


        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete([FromRoute] int id)
    {
        var poll = _pollService.Delete(id);

        return poll is false ? NotFound() : NoContent();
    }
}
