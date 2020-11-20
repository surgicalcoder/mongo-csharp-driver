﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Stages;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;
using MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToPipelineTranslators
{
    public static class GroupByMethodToPipelineTranslator
    {
        // private static fields
        private static readonly MethodInfo[] __groupByMethods;

        // static constructor
        static GroupByMethodToPipelineTranslator()
        {
            __groupByMethods = new[]
            {
                QueryableMethod.GroupByWithKeySelector,
                QueryableMethod.GroupByWithKeySelectorAndElementSelector,
                QueryableMethod.GroupByWithKeySelectorAndResultSelector,
                QueryableMethod.GroupByWithKeySelectorElementSelectorAndResultSelector
            };
        }

        // public static methods
        public static Pipeline Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__groupByMethods))
            {
                var sourceExpression = arguments[0];
                var pipeline = ExpressionToPipelineTranslator.Translate(context, sourceExpression);

                var keySelectorLambdaExpression = ExpressionHelper.Unquote(arguments[1]);
                var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelectorLambdaExpression, pipeline.OutputSerializer);
                var keySerializer = keySelectorTranslation.Serializer;

                if (method.Is(QueryableMethod.GroupByWithKeySelector))
                {
                    var elementSerializer = pipeline.OutputSerializer;
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        new AstProjectStage(
                            new AstProjectStageComputedFieldSpecification(new AstComputedField("_key", keySelectorTranslation.Ast)),
                            new AstProjectStageComputedFieldSpecification(new AstComputedField("_element", new AstFieldExpression("$ROOT"))),
                            new AstProjectStageExcludeIdSpecification()),
                        new AstGroupStage(
                            new AstFieldExpression("_key"),
                            new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, new AstFieldExpression("_element")))));

                    return pipeline;
                }

                if (method.Is(QueryableMethod.GroupByWithKeySelectorAndElementSelector))
                {
                    var elementSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[2]);
                    var elementSelectorContext = context.WithSymbolAsCurrent(elementSelectorLambdaExpression.Parameters[0], new Symbol("$ROOT", pipeline.OutputSerializer));
                    var elementSelectorTranslation = ExpressionToAggregationExpressionTranslator.Translate(elementSelectorContext, elementSelectorLambdaExpression.Body);
                    var elementSerializer = elementSelectorTranslation.Serializer;
                    var groupingSerializer = IGroupingSerializer.Create(keySerializer, elementSerializer);

                    pipeline.AddStages(
                        groupingSerializer,
                        new AstGroupStage(
                            keySelectorTranslation.Ast,
                            new[] { new AstComputedField("_elements", new AstUnaryExpression(AstUnaryOperator.Push, elementSelectorTranslation.Ast)) }));

                    return pipeline;
                }

                if (method.Is(QueryableMethod.GroupByWithKeySelectorAndResultSelector))
                {
                    var keyElementSerializer = AddKeyElementStage(context, pipeline, keySelectorTranslation);

                    var resultSelectorLambdaExpression = ExpressionHelper.Unquote(arguments[2]);
                    var keyParameter = resultSelectorLambdaExpression.Parameters[0];
                    var accumulatorFields = TranslateAccumulatorFields(context, resultSelectorLambdaExpression, keyParameter, keyElementSerializer, out var outputSerializer);

                    pipeline.AddStages(
                        outputSerializer,
                        new AstGroupStage(
                            id: new AstFieldExpression("_key"),
                            accumulatorFields),
                        new AstProjectStage(new AstProjectStageExcludeIdSpecification()));

                    return pipeline;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private methods
        private static IGroupByKeyElementSerializer AddKeyElementStage(TranslationContext context, Pipeline pipeline, LambdaExpression keySelectorLambda)
        {
            var keySelectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, keySelectorLambda, pipeline.OutputSerializer);
            return AddKeyElementStage(context, pipeline, keySelectorTranslation);
        }

        private static IGroupByKeyElementSerializer AddKeyElementStage(TranslationContext context, Pipeline pipeline, AggregationExpression keySelectorTranslation)
        {
            var elementAst = new AstFieldExpression("$ROOT");
            var elementSerializer = pipeline.OutputSerializer;
            if (elementSerializer is IWrappedValueSerializer wrappedValueSerializer)
            {
                elementAst = new AstFieldExpression(wrappedValueSerializer.FieldName);
                elementSerializer = wrappedValueSerializer.ValueSerializer;
            }

            var groupByKeyElementSerializer = GroupByKeyElementSerializer.Create(keySelectorTranslation.Serializer, elementSerializer);
            pipeline.AddStages(
                groupByKeyElementSerializer,
                new AstProjectStage(
                    new AstProjectStageComputedFieldSpecification(new AstComputedField("_key", keySelectorTranslation.Ast)),
                    new AstProjectStageComputedFieldSpecification(new AstComputedField("_element", elementAst)),
                    new AstProjectStageExcludeIdSpecification()));

            return groupByKeyElementSerializer;
        }

        private static List<AstComputedField> TranslateAccumulatorFields(TranslationContext context, LambdaExpression selectorLambda, ParameterExpression keyParameterExpression, IGroupByKeyElementSerializer keyElementSerializer, out IBsonSerializer outputSerializer)
        {
            var body = selectorLambda.Body;

            if (body is NewExpression newExpression)
            {
                return TranslateSelectorNewExpression(context, newExpression, keyParameterExpression, keyElementSerializer, out outputSerializer);
            }

            throw new ExpressionNotSupportedException(selectorLambda);
        }

        private static List<AstComputedField> TranslateSelectorNewExpression(TranslationContext context, NewExpression newExpression, ParameterExpression keyParameterExpression, IGroupByKeyElementSerializer keyElementSerializer, out IBsonSerializer outputSerializer)
        {
            var accumulatorFields = new List<AstComputedField>();
            var classMap = new BsonClassMap(newExpression.Type);

            for (var i = 0; i < newExpression.Members.Count; i++)
            {
                var member = newExpression.Members[i];
                var accumulatorExpression = newExpression.Arguments[i];
                var accumulatorTranslation = TranslateAccumulatorExpression(context, accumulatorExpression, keyParameterExpression, keyElementSerializer);
                var accumulatorComputedField = new AstComputedField(member.Name, accumulatorTranslation.Ast);
                accumulatorFields.Add(accumulatorComputedField);
                classMap.MapMember(member).SetSerializer(accumulatorTranslation.Serializer);
            }
            classMap.Freeze();

            var outputSerializerType = typeof(BsonClassMapSerializer<>).MakeGenericType(newExpression.Type);
            outputSerializer = (IBsonSerializer)Activator.CreateInstance(outputSerializerType, classMap);

            return accumulatorFields;
        }

        private static AggregationExpression TranslateAccumulatorExpression(TranslationContext context, Expression accumulatorExpression, ParameterExpression keyParameterExpression, IGroupByKeyElementSerializer keyElementSerializer)
        {
            if (accumulatorExpression == keyParameterExpression)
            {
                var ast = new AstUnaryExpression(AstUnaryOperator.First, new AstFieldExpression("_key"));
                return new AggregationExpression(accumulatorExpression, ast, keyElementSerializer.KeySerializer);
            }

            if (accumulatorExpression is MemberExpression memberExpression)
            {
                var memberDeclaringType = memberExpression.Member.DeclaringType;
                if (memberDeclaringType.IsConstructedGenericType && memberDeclaringType.GetGenericTypeDefinition() == typeof(IGrouping<,>))
                {
                    if (memberExpression.Member.Name == "Key")
                    {
                        var ast = new AstUnaryExpression(AstUnaryOperator.First, new AstFieldExpression("_key"));
                        return new AggregationExpression(accumulatorExpression, ast, keyElementSerializer.KeySerializer);
                    }
                }
            }

            if (accumulatorExpression is MethodCallExpression methodCallExpression)
            {
                var method = methodCallExpression.Method;
                var arguments = methodCallExpression.Arguments;

                var sourceExpression = arguments[0];
                var sourceType = sourceExpression.Type;
                if (sourceType.IsConstructedGenericType)
                {
                    var sourceTypeDefinition = sourceType.GetGenericTypeDefinition();
                    if (sourceTypeDefinition == typeof(IEnumerable<>) || sourceTypeDefinition == typeof(IGrouping<,>))
                    {
                        if (method.Is(EnumerableMethod.Count))
                        {
                            var ast = new AstUnaryExpression(AstUnaryOperator.Sum, 1);
                            var serializer = new Int32Serializer();
                            return new AggregationExpression(accumulatorExpression, ast, serializer);
                        }

                        if (method.Name == "Min" && method.DeclaringType == typeof(Enumerable))
                        {
                            var selectorLambda = (LambdaExpression)arguments[1];
                            var elementSerializer = WrappedValueSerializer.Create("_element", keyElementSerializer.ElementSerializer);
                            var selectorTranslation = ExpressionToAggregationExpressionTranslator.TranslateLambdaBody(context, selectorLambda, elementSerializer);
                            var minAst = new AstUnaryExpression(AstUnaryOperator.Min, selectorTranslation.Ast);
                            return new AggregationExpression(accumulatorExpression, minAst, selectorTranslation.Serializer);
                        }
                    }
                }
            }

            throw new ExpressionNotSupportedException(accumulatorExpression);
        }
    }
}
