using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using todaapp.Data;
using todaapp.Models;

namespace todaapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TodoController : ControllerBase
    {
        public readonly TodoDbContext _Db;
        public TodoController(TodoDbContext db)
        {
            this._Db = db;

        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems()
        {
            var items = await _Db.Items.ToListAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(Item data)
        {
            if (ModelState.IsValid)
            {
                await _Db.Items.AddAsync(data);
                await _Db.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { Id = data.Id }, data);
            }
            return new JsonResult("something went wrong")
            {
                StatusCode = 500
            };
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Item>> GetById(int id)
        {
            var item = await _Db.Items.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null)
            {
                return NotFound();
            }
            return Ok(item);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, Item data)
        {
            if (id != data.Id) return BadRequest();
            var item = await _Db.Items.FirstOrDefaultAsync(x => x.Id == id);
            if (item is null) return NotFound();
            item.Title = data.Title;
            item.Description = data.Description;
            item.Done = data.Done;
            //context let us modify the object and then save it as an update
            await _Db.SaveChangesAsync();
            return NoContent();

        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletItem(int id)
        {
            var existItem = await _Db.Items.FirstOrDefaultAsync(x => x.Id == id);
            if (existItem is null) return NotFound();
            _Db.Items.Remove(existItem);
            await _Db.SaveChangesAsync();
            return Ok(existItem);
        }
    }
}