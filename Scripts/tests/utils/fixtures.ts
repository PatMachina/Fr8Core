﻿module dockyard.tests.utils {

    export class Fixtures {
        public static newProcessTemplate = <interfaces.IProcessTemplateVM> {
            name: 'Test',
            description: 'Description',
            processTemplateState: 1
        };

        public static updatedProcessTemplate = <interfaces.IProcessTemplateVM> {
            'name': 'Updated',
            'description': 'Description',
            'processTemplateState': 1,
            'subscribedDocuSignTemplates': ['58521204-58af-4e65-8a77-4f4b51fef626']
        }

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
        }

        public static configurationStore = {
            "fields":
            [
                {
                    "type": "textField",
                    "name": "connection_string",
                    "required": true,
                    "value": "",
                    "fieldLabel": "SQL Connection String"
                },
                {
                    "type": "textField",
                    "name": "query",
                    "required": true,
                    "value": "",
                    "fieldLabel": "Custom SQL Query"
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions",
                    "selected": false,
                    "fieldLabel": "Log All Transactions?"
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions1",
                    "selected": false,
                    "fieldLabel": "Log Some Transactions?"
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions2",
                    "selected": false,
                    "fieldLabel": "Log No Transactions?"
                },
                {
                    "type": "checkboxField",
                    "name": "log_transactions3",
                    "selected": false,
                    "fieldLabel": "Log Failed Transactions?"
                }
            ]
        };

    }
}
