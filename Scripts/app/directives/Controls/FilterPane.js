/// <reference path="../../_all.ts" />
var dockyard;
(function (dockyard) {
    var directives;
    (function (directives) {
        'use strict';
        function FilterPane() {
            return {
                restrict: 'E',
                templateUrl: '/AngularTemplate/FilterPane',
                scope: {
                    currentAction: '=',
                    field: '='
                },
                controller: ['$scope', '$timeout', 'CrateHelper',
                    function ($scope, $timeout, crateHelper) {
                        $scope.operators = [
                            { text: '>', value: 'gt' },
                            { text: '>=', value: 'gte' },
                            { text: '<', value: 'lt' },
                            { text: '<=', value: 'lte' },
                            { text: '==', value: 'eq' },
                            { text: '<>', value: 'neq' }
                        ];
                        $scope.defaultOperator = '';
                        $scope.$watch('currentAction', function (newValue) {
                            if (newValue && newValue.crateStorage) {
                                var crate = crateHelper.findByManifestTypeAndLabel(newValue.crateStorage, 'Standard Design-Time Fields', 'Queryable Criteria');
                                $scope.fields = [];
                                if (crate != null) {
                                    var crateJson = angular.fromJson(crate.contents);
                                    angular.forEach(crateJson.Fields, function (it) {
                                        $scope.fields.push({ name: it.Key, key: it.Key });
                                    });
                                }
                            }
                        });
                        $scope.$watch('field', function (newValue) {
                            if (newValue && newValue.value) {
                                var jsonValue = angular.fromJson(newValue.value);
                                $scope.conditions = jsonValue.conditions;
                                $scope.executionType = jsonValue.executionType;
                            }
                            else {
                                $scope.conditions = [
                                    new dockyard.model.Condition(null, $scope.defaultOperator, null)
                                ];
                                $scope.executionType = 1;
                            }
                        });
                        var updateFieldValue = function () {
                            $scope.field.value = angular.toJson({
                                executionType: $scope.executionType,
                                conditions: $scope.conditions
                            });
                        };
                        $scope.$watch('conditions', function () {
                            updateFieldValue();
                        }, true);
                        $scope.$watch('executionType', function () {
                            updateFieldValue();
                        });
                    }
                ]
            };
        }
        directives.FilterPane = FilterPane;
    })(directives = dockyard.directives || (dockyard.directives = {}));
})(dockyard || (dockyard = {}));
app.directive('filterPane', dockyard.directives.FilterPane);
//# sourceMappingURL=FilterPane.js.map