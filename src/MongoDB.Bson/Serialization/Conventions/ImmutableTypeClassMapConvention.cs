/* Copyright 2016-present MongoDB Inc.
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
* 
*/

using System;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Bson.Serialization.Conventions
{
    /// <summary>
    /// Maps a fully immutable type. This will include anonymous types.
    /// </summary>
    public class ImmutableTypeClassMapConvention : ConventionBase, IClassMapConvention, IPostProcessingConvention
    {
        /// <inheritdoc />
        public void Apply(BsonClassMap classMap)
        {
            var typeInfo = classMap.ClassType.GetTypeInfo();

            if (typeInfo.GetConstructor(Type.EmptyTypes) != null)
            {
                return;
            }

            var properties = GetProperties(typeInfo);
            if (properties.Any(CanWrite))
            {
                return; // a type that has any writable properties is not immutable
            }

            var anyConstructorsWereFound = false;
            var constructorBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            foreach (var ctor in typeInfo.GetConstructors(constructorBindingFlags))
            {
                if (ctor.IsPrivate)
                {
                    continue; // do not consider private constructors
                }

                var parameters = ctor.GetParameters();
                if (parameters.Length != properties.Length)
                {
                    continue; // only consider constructors that have sufficient parameters to initialize all properties
                }

                var matches = parameters
                    .GroupJoin(properties,
                        parameter => parameter.Name,
                        property => property.Name,
                        (parameter, props) => new { Parameter = parameter, Properties = props },
                        StringComparer.OrdinalIgnoreCase);

                if (matches.Any(m => m.Properties.Count() != 1))
                {
                    continue;
                }

                if (ctor.IsPublic && !typeInfo.IsAbstract)
                {
                    // we need to save constructorInfo only for public constructors in non abstract classes
                    classMap.MapConstructor(ctor);
                }

                anyConstructorsWereFound = true;
            }

            if (anyConstructorsWereFound)
            {
                // if any constructors were found by this convention
                // then map all the properties from the ClassType inheritance level also
                foreach (var property in properties)
                {
                    MapPropertyIfPossible(classMap, property);
                }
            }
        }

        /// <summary>
        /// Applies a post processing modification to the class map.
        /// </summary>
        /// <param name="classMap">The class map.</param>
        public void PostProcess(BsonClassMap classMap)
        {
            var typeInfo = classMap.ClassType.GetTypeInfo();

            var properties = GetProperties(typeInfo);

            if (properties.Length == 0 || // no properties to map
                properties.Any(CanWrite)) // a type that has any writable properties is not immutable
            {
                return;
            }

            // Try to map properties that were not added in the Apply,
            // because now we possibly can have constructors that could be configured in different conventions
            // so, we need to add the related properties for them
            foreach (var creator in classMap.CreatorMaps.Where(m => m.Arguments != null))
            {
                foreach (var argument in creator.Arguments)
                {
                    if (!classMap.DeclaredMemberMaps.Any(d => CompareWithIgnoreCase(d.ElementName, argument.Name)))
                    {
                        var notMappedProperty = properties.FirstOrDefault(p => CompareWithIgnoreCase(p.Name, argument.Name));
                        if (notMappedProperty != null)
                        {
                            MapPropertyIfPossible(classMap, notMappedProperty);
                        }
                    }
                }
            }

            bool CompareWithIgnoreCase(string value1, string value2)
            {
                return value1.Equals(value2, StringComparison.OrdinalIgnoreCase);
            }
        }

        // private methods
        private bool CanWrite(PropertyInfo propertyInfo)
        {
            // CanWrite gets true even if a property has only a private setter
            return propertyInfo.CanWrite && (propertyInfo.SetMethod?.IsPublic ?? false);
        }

        private PropertyInfo[] GetProperties(TypeInfo typeInfo)
        {
            var propertyBindingsFlags = BindingFlags.Public | BindingFlags.Instance;

            return
                typeInfo.GetProperties(propertyBindingsFlags)
                .Where(p => p.GetCustomAttribute<BsonIgnoreAttribute>() == null)
                .ToArray();
        }

        private void MapPropertyIfPossible(BsonClassMap classMap, PropertyInfo property)
        {
            if (property.DeclaringType != classMap.ClassType)
            {
                return;
            }

            var memberMap = classMap.MapMember(property);
            if (classMap.IsAnonymous)
            {
                var defaultValue = memberMap.DefaultValue;
                memberMap.SetDefaultValue(defaultValue);
            }
        }
    }
}
