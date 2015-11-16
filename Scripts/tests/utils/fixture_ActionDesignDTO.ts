﻿
module dockyard.tests.utils.fixtures {

    export class ActionDesignDTO {

        public static newProcessTemplate = <interfaces.IRouteVM> {
            name: 'Test',
            description: 'Description',
            routeState: 1
        };

        public static filePickerField: model.FileControlDefinitionDTO = {
            type: 'FilePicker',
            fieldLabel: 'FilePicker Test',
            name: 'FilePickerTest',
            events: [],
            value: null
        };

        public static textField: model.TextBoxControlDefinitionDTO = {
            required: true,
            type: 'TextBox',
            fieldLabel: 'test',
            name: 'test',
            events: [],
            value: 'test'
        };

        public static textBlock: model.TextBlock = new model.TextBlock('<span>teststs</span>', 'well well-lg');

        public static dropDownListBox: model.DropDownListControlDefinitionDTO = {
            listItems: [{ key: 'test1', value: 'value1' }, { key: 'test2', value: 'value2' }, { key: 'test3', value: 'value3' }],
            source: {
                manifestType: 'testManifest',
                label: 'testLabel'
            },
            type: 'DropDownList',
            fieldLabel: 'DropDownList Test',
            name: 'DropDownList',
            events: [],
            value: 'value3'
        };
        /*
        public static radioButtonGroupField: model.RadioButtonGroupControlDefinitionDTO = {
            groupName: 'testGroup',
            radios: [],
            type: 'RadioButtonGroup',
            fieldLabel: 'RadioButtonGroup Test',
            name: 'RadioButtonGroupTest',
            events: [],
            value: null
        };*/

        public static radioButtonGroupField: model.RadioButtonGroupControlDefinitionDTO = {
            groupName: 'SMSNumber_Group',
            radios: [
                {
                    selected: false,
                    name: 'SMSNumberOption',
                    value: 'SMS Number',
                    type: "RadioButtonGroup",
                    fieldLabel: null,
                    events: null,
                    controls: [
                        {
                            name: 'SMS_Number',
                            value: null,
                            fieldLabel: null,
                            type: "TextBox",
                            events: null
                        }
                    ]
                },
                {
                    selected: false,
                    name: 'SMSNumberOption',
                    value: 'A value from Upstream Crate',
                    type: "RadioButtonGroup",
                    fieldLabel: null,
                    events: null,
                    controls: [
                        {
                            name: 'SMS_Number2',
                            value: null,
                            fieldLabel: null,
                            type: "TextBox",
                            events: null
                        }/*
                        <model.DropDownListControlDefinitionDTO>{
                            'listItems': [],
                            'name': 'upstream_crate',
                            'required': false,
                            'value': null,
                            "fieldLabel": null,
                            "type": "DropDownList",
                            "selected": false,
                            "events": [
                                {
                                    "name": "onChange",
                                    "handler": "requestConfig"
                                }
                            ],
                            source: null
                        }*/
                    ]
                }
            ],
            name: '',
            value: null,
            fieldLabel: "For the SMS Number use:",
            type: "RadioButtonGroup",
            events: null
        };

        public static configurationControls = {
            "fields":
            [
                {
                    "type": "textField",
                    "name": "connection_string",
                    "required": true,
                    "value": "",
                    "fieldLabel": "SQL Connection String",
                    "events": []
                },
                {
                    "type": "textField",
                    "name": "query",
                    "required": true,
                    "value": "",
                    "fieldLabel": "Custom SQL Query",
                    "events": []
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions",
                    "selected": false,
                    "value": "",
                    "fieldLabel": "Log All Transactions?",
                    "events": []
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions1",
                    "selected": false,
                    "value": "",
                    "fieldLabel": "Log Some Transactions?",
                    "events": []
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions2",
                    "selected": false,
                    "value": "",
                    "fieldLabel": "Log No Transactions?",
                    "events": []
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions3",
                    "selected": false,
                    "value": "",
                    "fieldLabel": "Log Failed Transactions?",
                    "events": []
                }
            ]
        };

        public static fieldMappingSettings = {
            "fields": [
                {
                    "name": "[_AccessLevelTemplate].Value]",
                    "value": "Text"
                },
                {
                    "name": "[_AccessLevelTemplate].Version]",
                    "value": "Checkbox"
                }
            ]
        };

        public static noAuthActionVM = <interfaces.IActionVM> {
            crateStorage: {
                crates: [{
                    id: "37ea608f-eead-4d0f-b75f-8033474e6030",
                    label: "Configuration_Controls",
                    contents: angular.fromJson("{\"Controls\":[{\"name\":\"connection_string\",\"required\":true,\"value\":null,\"label\":\"SQL Connection String\",\"type\":\"TextBox\",\"selected\":false,\"events\":[{\"name\":\"onChange\",\"handler\":\"requestConfig\"}],\"source\":null}],\"ManifestType\":6,\"ManifestId\":6,\"ManifestName\":\"Standard Configuration Controls\"}"),
                    parentCrateId: null,
                    manifestType: "Standard Configuration Controls",                    
                    manufacturer: null
                }]
            },
            configurationControls: {
                fields: [{
                    fieldLabel: "SQL Connection String",
                    name: "connection_string",
                    value: null
                }]
            },
            activityTemplate: {
                id: 2
            },
            isTempId: false,
            currentView: null,
            id: 81,
            name: "Write_To_Sql_Server"
        };

        public static internalAuthActionVM = <interfaces.IActionVM> {
            crateStorage: {
                crates: [{
                    id: "37ea608f-eead-4d0f-b75f-8033474e6030",
                    label: "Configuration_Controls",
                    contents: angular.fromJson("{\"Controls\":[{\"name\":\"connection_string\",\"required\":true,\"value\":null,\"label\":\"SQL Connection String\",\"type\":\"TextBox\",\"selected\":false,\"events\":[{\"name\":\"onChange\",\"handler\":\"requestConfig\"}],\"source\":null}],\"ManifestType\":6,\"ManifestId\":6,\"ManifestName\":\"Standard Configuration Controls\"}"),
                    parentCrateId: null,
                    manifestType: "Standard Configuration Controls",
                    manufacturer: null
                }, {
                        id: "37ea608f-eead-4d0f-b75f-8033474e6030",
                        label: "Test_Auth_Crate",
                        contents: "{\"Mode\":\"1\"}",
                        parentCrateId: null,
                        manifestType: "Standard Authentication",
                        manufacturer: null
                    }]
            },
            configurationControls: {
                fields: [{
                    fieldLabel: "SQL Connection String",
                    name: "connection_string",
                    value: null
                }]
            },
            activityTemplate: {
                id: 2
            },
            isTempId: false,
            currentView: null,
            id: 81,
            name: "Write_To_Sql_Server"
        };

        public static externalAuthActionVM = <interfaces.IActionVM> {
            crateStorage: {
                crates: [{
                    id: "37ea608f-eead-4d0f-b75f-8033474e6030",
                    label: "Configuration_Controls",
                    contents: angular.fromJson("{\"Controls\":[{\"name\":\"connection_string\",\"required\":true,\"value\":null,\"label\":\"SQL Connection String\",\"type\":\"TextBox\",\"selected\":false,\"events\":[{\"name\":\"onChange\",\"handler\":\"requestConfig\"}],\"source\":null}],\"ManifestType\":6,\"ManifestId\":6,\"ManifestName\":\"Standard Configuration Controls\"}"),
                    parentCrateId: null,
                    manifestType: "Standard Configuration Controls",
                    manufacturer: null
                }, {
                        id: "37ea608f-eead-4d0f-b75f-8033474e6030",
                        label: "Test_Auth_Crate",
                        contents: "{\"Mode\":\"2\"}",
                        parentCrateId: null,
                        manifestType: "Standard Authentication",
                        manufacturer: null
                    }]
            },
            configurationControls: {
                fields: [{
                    fieldLabel: "SQL Connection String",
                    name: "connection_string",
                    value: null
                }]
            },
            activityTemplate: {
                id: 2
            },
            isTempId: false,
            currentView: null,
            id: 81,
            name: "Write_To_Sql_Server"
        };

        /*
        public static paneConfiguration = <dockyard.directives.paneConfigureAction.IPaneConfigureActionScope> {
            currentAction: ActionDesignDTO.actionDesignDTO
        };

        public static PaneConfigureActionOnRender_EventArgs = <dockyard.directives.paneConfigureAction.RenderEventArgs> {
             action: ActionDesignDTO.actionDesignDTO
        };

        public static PaneConfigureActionOnRender_Event = <ng.IAngularEvent> {
            currentScope: ActionDesignDTO.paneConfiguration,
            targetScope: ActionDesignDTO.paneConfiguration,
            defaultPrevented: null,
            name: "",
            preventDefault: null,
            stopPropagation: null
        };*/
    }
} 