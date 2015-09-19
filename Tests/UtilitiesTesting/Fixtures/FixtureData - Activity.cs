﻿using Core.Interfaces;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Data.Wrappers;
using Newtonsoft.Json;
using StructureMap;
using System.Collections.Generic;
using System;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {

        public static ActivityDO TestActivity57()
        {
            return new ActivityDO
            {
                Id = 57,
                Ordering = 2,
                ParentActivityId = 54
            };

        }

        public static ActivityDO TestActivityTree()
        {
            return new ActivityDO
            {
                Id = 1,
                Ordering = 1,
                Activities = new List<ActivityDO>
                {
                    new ActivityDO
                    {
                        Id = 23,
                        Ordering = 1,
                        ParentActivityId = 1
                    },
                    new ActivityDO
                    {
                        Id = 43,
                        ParentActivityId = 1,
                        Ordering = 2,
                        Activities = new List<ActivityDO>
                        {
                            new ActivityDO
                            {
                                Id = 44,
                                Ordering = 1,
                                ParentActivityId = 43
                            },
                            new ActivityDO
                            {
                                Id = 46,
                                Ordering = 2,
                                ParentActivityId = 43
                            },
                            new ActivityDO
                            {
                                Id = 48,
                                Ordering = 3,
                                ParentActivityId = 43
                            },

                        }
                    },
                    new ActivityDO
                    {
                        Id = 52,
                        Ordering = 3,
                        ParentActivityId = 1,
                        Activities = new List<ActivityDO>
                        {
                            new ActivityDO
                            {
                                Id = 53,
                                Ordering = 1,
                                ParentActivityId = 52
                            },
                            new ActivityDO
                            {
                                Id = 54,
                                ParentActivityId = 52,
                                Ordering = 2,

                                Activities = new List<ActivityDO>
                                {
                                    new ActivityDO
                                    {
                                        Id = 56,
                                        ParentActivityId = 54,
                                        Ordering = 1
                                    },
                                    new ActivityDO
                                    {
                                        Id = 57,
                                        ParentActivityId = 54,
                                        Ordering = 2
                                    },
                                    new ActivityDO
                                    {
                                        Id = 58,
                                        ParentActivityId = 54,
                                        Ordering = 3
                                    },

                                }
                            },
                            new ActivityDO
                            {
                                Id = 55,
                                ParentActivityId = 52,
                                Ordering = 3
                            },

                        }
                    },
                    new ActivityDO
                    {
                        Id = 59,
                        Ordering = 4,
                        ParentActivityId = 1,
                        Activities = new List<ActivityDO>
                        {
                            new ActivityDO
                            {
                                Id = 60,
                                ParentActivityId = 59,
                                Ordering = 1
                            },
                            new ActivityDO
                            {
                                Id = 61,
                                ParentActivityId = 59,
                                Ordering = 2,

                                Activities = new List<ActivityDO>
                                {
                                    new ActivityDO
                                    {
                                        Id = 63,
                                        ParentActivityId = 61,
                                        Ordering = 1
                                    },
                                    new ActivityDO
                                    {
                                        Id = 64,
                                        ParentActivityId = 61,
                                        Ordering = 2
                                    },
                                    new ActivityDO
                                    {
                                        Id = 65,
                                        ParentActivityId = 61,
                                        Ordering = 3
                                    },
                                }
                            },

                            new ActivityDO
                            {
                                Id = 62,
                                ParentActivityId = 59,
                                Ordering = 3
                            },
                        },

                    }
                }
            };
        }



    }
}