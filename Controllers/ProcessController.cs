﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using StructureMap;
using Core.Interfaces;
using Core.Services;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Microsoft.AspNet.Identity;

namespace Web.Controllers
{
    [Authorize]
    [RoutePrefix("api/processes")]
    public class ProcessController : ApiController
    {
        private readonly IProcess _process;


        public ProcessController()
        {
            _process = ObjectFactory.GetInstance<IProcess>();
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curProcessDO = uow.ProcessRepository.GetByKey(id);
                var curPayloadDTO = new PayloadDTO(curProcessDO.CrateStorage, id);

                EventManager.ProcessRequestReceived(curProcessDO);

                return Ok(curPayloadDTO);
            }
        }

        [Route("getIdsByName")]
        [HttpGet]
        public IHttpActionResult GetIdsByName(string name)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var processIds = uow.ProcessRepository.GetQuery().Where(x=>x.Name == name).Select(x=>x.Id).ToArray();
                
                return Json(processIds);
            }
        }

        [Route("launch")]
        [HttpPost]
        public async Task<IHttpActionResult> Launch(int processTemplateId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var processTemplateDO = uow.ProcessTemplateRepository.GetByKey(processTemplateId);
                await _process.Launch(processTemplateDO, null);

                return Ok();
            }
        }

        // Return the Processes accordingly to ID given 
        [Route("get/{id:int?}")]
        [HttpGet]
        public IHttpActionResult Get(int? id = null)
        {
            var curProcess = _process.GetProcessOfAccount(User.Identity.GetUserId(), User.IsInRole(Roles.Admin), id);

            if (curProcess.Any())
            {
                if (id.HasValue)
                {
                    return Ok(Mapper.Map<ProcessDTO>(curProcess.First()));
                }

                return Ok(curProcess.Select(Mapper.Map<ProcessDTO>));
            }
            return Ok();
        }

       //NOTE: IF AND WHEN THIS CLASS GETS USED, IT NEEDS TO BE FIXED TO USE OUR 
       //STANDARD UOW APPROACH, AND NOT CONTACT THE DATABASE TABLE DIRECTLY.

        //private DockyardDbContext db = new DockyardDbContext();
        // GET: api/Process
        //public IQueryable<ProcessDO> Get()
        //{
        //    return db.Processes;
        //}

        //// GET: api/Process/5
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult GetProcess(int id)
        //{
        //    ProcessDO processDO = db.Processes.Find(id);
        //    if (processDO == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(processDO);
        //}

        //// PUT: api/Process/5
        //[ResponseType(typeof(void))]
        //public IHttpActionResult PutProcess(int id, ProcessDO processDO)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != processDO.Id)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(processDO).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!ProcessDOExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/Process
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult PostProcessDO(ProcessDO processDO)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.Processes.Add(processDO);
        //    db.SaveChanges();

        //    return CreatedAtRoute("DefaultApi", new { id = processDO.Id }, processDO);
        //}

        //// DELETE: api/Process/5
        //[ResponseType(typeof(ProcessDO))]
        //public IHttpActionResult DeleteProcessDO(int id)
        //{
        //    ProcessDO processDO = db.Processes.Find(id);
        //    if (processDO == null)
        //    {
        //        return NotFound();
        //    }

        //    db.Processes.Remove(processDO);
        //    db.SaveChanges();

        //    return Ok(processDO);
        //}

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        //private bool ProcessDOExists(int id)
        //{
        //    return db.Processes.Count(e => e.Id == id) > 0;
        //}
    }
}