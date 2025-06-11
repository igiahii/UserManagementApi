using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
     [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private static readonly List<User> users = new();
        private static int nextId = 1;

        [HttpGet]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            try
            {
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error retrieving users: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public ActionResult<User> GetUser(int id)
        {
            try
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                return user is null ? NotFound($"User with ID {id} not found.") : Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error fetching user: {ex.Message}");
            }
        }

        [HttpPost]
        public ActionResult<User> CreateUser([FromBody] UserCreateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = new User
                {
                    Id = nextId++,
                    FullName = userDto.FullName,
                    Email = userDto.Email,
                    Department = userDto.Department
                };
                users.Add(user);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error creating user: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public ActionResult UpdateUser(int id, [FromBody] UserCreateDto userDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null)
                    return NotFound($"User with ID {id} not found.");

                user.FullName = userDto.FullName;
                user.Email = userDto.Email;
                user.Department = userDto.Department;

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error updating user: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null)
                    return NotFound($"User with ID {id} not found.");

                users.Remove(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, $"Error deleting user: {ex.Message}");
            }
        }
    }
}