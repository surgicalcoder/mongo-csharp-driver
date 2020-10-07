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

using System.Linq.Expressions;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class SelectMethodTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (expression.Method.Is(EnumerableMethod.Select))
            {
                var sourceExpression = expression.Arguments[0];
                var selectorExpression = (LambdaExpression)expression.Arguments[1];

                var sourceTranslation = ExpressionTranslator.Translate(context, sourceExpression);
                var selectorParameter = selectorExpression.Parameters[0];
                var selectorParameterSerializer = ArraySerializerHelper.GetItemSerializer(sourceTranslation.Serializer);
                var selectorContext = context.WithSymbol(selectorParameter, new Symbol("$" + selectorParameter.Name, selectorParameterSerializer));
                var translatedSelector = ExpressionTranslator.Translate(selectorContext, selectorExpression.Body);
                var ast = AstMapExpression.Create(
                    sourceTranslation.Ast,
                    selectorParameter.Name,
                    translatedSelector.Ast);
                var serializer = IEnumerableSerializer.Create(translatedSelector.Serializer);

                return new ExpressionTranslation(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}