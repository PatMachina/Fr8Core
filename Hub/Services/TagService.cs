﻿using Data.Entities;
using Data.Interfaces;
using Data.Utility;
using Data.Utility.JoinClasses;
using Hub.Interfaces;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hub.Services
{
    public class TagService : ITag
    {
        public IList<Tag> GetAllTags()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var tags = new List<Tag>();
                tags = uow.TagRepository.GetAll().ToList();
                return tags;
            }
        }

        public Tag GetTag(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.TagRepository.GetByKey(id);
            }
        }

        public Tag GetTagByKey(string key)
        {
            Tag tag = null;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var result = uow.TagRepository.FindOne(x => x.Key == key);
                if (!object.Equals(result, default(Tag)))
                {
                    tag = result;
                }
            }
            return tag;
        }

        public IList<Tag> GetTags(int fileDoId)
        {
            var tags = new List<Tag>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var fileTags = uow.FileTagsRepository.FindList(x => x.FileDoId == fileDoId);
                foreach (var fileTag in fileTags)
                {
                    tags.Add(fileTag.Tag);
                }
            }
            return tags;
        }

        public IList<FileDO> GetFiles(Tag tag)
        {
            return GetFiles(tag.Id);
        }

        public IList<FileDO> GetFiles(int tagId)
        {
            var files = new List<FileDO>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var tag = uow.TagRepository.GetByKey(tagId);
                if (tag != null)
                {
                    var fileTags = uow.FileTagsRepository.FindList(x => x.TagId == tag.Id);
                    foreach (var fileTag in fileTags)
                    {
                        files.Add(fileTag.File);
                    }
                }
            }
            return files;
        }

        public void UpdateTags(int fileDoId, IList<Tag> tags)
        {   
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var tagRepository = uow.TagRepository;
                var fileTagRepository = uow.FileTagsRepository;
                // update old tags and add new
                foreach (var tag in tags)
                {   
                    var dbTag = tagRepository.FindOne(x => x.Key == tag.Key);
                    if (!object.Equals(dbTag, default(Tag)))
                    {
                        dbTag.Value = tag.Value;
                    }
                    else
                    {
                        tagRepository.Add(tag); 
                    }
                }
                uow.SaveChanges();

                // remove all fileTags with fileDoId
                RemoveFileTags(fileDoId);

                // Add new fileTags to the fileDo
                foreach (var tag in tags)
                {
                    var dbTag = tagRepository.FindOne(x => x.Key == tag.Key);
                    if (!object.Equals(dbTag, default(Tag)))
                    {
                        var fileTag = new FileTags()
                        {
                            FileDoId = fileDoId,
                            TagId = dbTag.Id
                        };

                        fileTagRepository.Add(fileTag);
                    }
                }
                uow.SaveChanges();
            }
        }

        void RemoveFileTags(int fileDoId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var fileTagsRepository = uow.FileTagsRepository;
                var listFileTags = fileTagsRepository.FindList(x => x.FileDoId == fileDoId);
                foreach (var fileTag in listFileTags)
                {
                    fileTagsRepository.Remove(fileTag);
                }

                uow.SaveChanges();
            }
        }
    }
}
