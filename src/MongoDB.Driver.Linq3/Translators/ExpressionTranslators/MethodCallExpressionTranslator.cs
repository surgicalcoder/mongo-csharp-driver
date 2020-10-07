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
using MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators;
using MongoDB.Driver.Linq3.Translators.PipelineTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class MethodCallExpressionTranslator
    {
        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            switch (expression.Method.Name)
            {
                case "Abs": return AbsMethodTranslator.Translate(context, expression);
                case "Aggregate": return AggregateMethodTranslator.Translate(context, expression);
                case "All": return AllMethodTranslator.Translate(context, expression);
                case "Any": return AnyMethodTranslator.Translate(context, expression);
                case "Average": return AverageMethodTranslator.Translate(context, expression);
                case "Ceiling": return CeilingMethodTranslator.Translate(context, expression);
                case "Concat": return ConcatMethodTranslator.Translate(context, expression);
                case "CompareTo": return CompareToMethodTranslator.Translate(context, expression);
                case "Contains": return ContainsMethodTranslator.Translate(context, expression);
                case "Distinct": return DistinctMethodTranslator.Translate(context, expression);
                case "ElementAt": return ElementAtMethodTranslator.Translate(context, expression);
                case "Equals": return EqualsMethodTranslator.Translate(context, expression);
                case "Except": return ExceptMethodTranslator.Translate(context, expression);
                case "Exp": return ExpMethodTranslator.Translate(context, expression);
                case "Floor": return FloorMethodTranslator.Translate(context, expression);
                case "get_Item": return GetItemMethodTranslator.Translate(context, expression);
                case "Intersect": return IntersectMethodTranslator.Translate(context, expression);
                case "IsNullOrEmpty": return IsNullOrEmptyMethodTranslator.Translate(context, expression);
                case "IsSubsetOf": return IsSubsetOfMethodTranslator.Translate(context, expression);
                case "Parse": return ParseMethodTranslator.Translate(context, expression);
                case "Pow": return PowMethodTranslator.Translate(context, expression);
                case "Range": return RangeMethodTranslator.Translate(context, expression);
                case "Reverse": return ReverseMethodTranslator.Translate(context, expression);
                case "Select": return SelectMethodTranslator.Translate(context, expression);
                case "SetEquals": return SetEqualsMethodTranslator.Translate(context, expression);
                case "Split": return SplitMethodTranslator.Translate(context, expression);
                case "Sqrt": return SqrtMethodTranslator.Translate(context, expression);
                case "StrLenBytes": return StrLenBytesMethodTranslator.Translate(context, expression);
                case "Sum": return SumMethodTranslator.Translate(context, expression);
                case "Take": return TakeMethodTranslator.Translate(context, expression);
                case "ToString": return ToStringMethodTranslator.Translate(context, expression);
                case "Truncate": return TruncateMethodTranslator.Translate(context, expression);
                case "Where": return WhereMethodTranslator.Translate(context, expression);
                case "Union": return UnionMethodTranslator.Translate(context, expression);
                case "Zip": return ZipMethodTranslator.Translate(context, expression);

                case "Count":
                case "LongCount":
                    return CountMethodTranslator.Translate(context, expression);

                case "First":
                case "Last":
                    return FirstLastMethodTranslator.Translate(context, expression);

                case "IndexOf":
                case "IndexOfBytes":
                    return IndexOfMethodTranslator.Translate(context, expression);

                case "Log":
                case "Log10":
                    return LogMethodTranslator.Translate(context, expression);

                case "Max":
                case "Min":
                    return MaxMinMethodTranslator.Translate(context, expression);

                case "StandardDeviationPopulation":
                case "StandardDeviationSample":
                    return StandardDeviationMethodsTranslator.Translate(context, expression);

                case "Substring":
                case "SubstrBytes":
                    return SubstringMethodTranslator.Translate(context, expression);

                case "ToLower":
                case "ToLowerInvariant":
                case "ToUpper":
                case "ToUpperInvariant":
                    return ToLowerToUpperMethodTranslator.Translate(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}