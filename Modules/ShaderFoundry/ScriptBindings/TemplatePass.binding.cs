// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using PassIdentifier = UnityEngine.Rendering.PassIdentifier;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/TemplatePass.h")]
    internal struct TemplatePassInternal
    {
        internal FoundryHandle m_UsePassNameHandle;
        internal FoundryHandle m_DisplayNameHandle;
        internal FoundryHandle m_ReferenceNameHandle;
        internal FoundryHandle m_PassIdentifierHandle;
        internal FoundryHandle m_VertexStageElementListHandle;
        internal FoundryHandle m_FragmentStageElementListHandle;
        internal FoundryHandle m_CommandDescriptorListHandle;
        internal FoundryHandle m_PragmaDescriptorListHandle;
        internal FoundryHandle m_TagDescriptorListHandle;
        internal bool m_EnableDebugging;

        internal extern static TemplatePassInternal Invalid();
        internal extern bool IsValid();
        internal extern bool IsUsePass();
    }

    [FoundryAPI]
    internal readonly struct TemplatePass : IEquatable<TemplatePass>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplatePassInternal templatePass;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public bool IsUsePass => (IsValid && templatePass.IsUsePass());
        public bool IsNormalPass => (IsValid && !templatePass.IsUsePass());
        public string UsePassName => container?.GetString(templatePass.m_UsePassNameHandle) ?? string.Empty;
        public string DisplayName => container?.GetString(templatePass.m_DisplayNameHandle) ?? string.Empty;
        public string ReferenceName => container?.GetString(templatePass.m_ReferenceNameHandle) ?? string.Empty;
        // TODO SHADER: The else case should return invalid pass identifier once it's possible to construct this in managed.
        public PassIdentifier PassIdentifier => container?.GetPassIdentifier(templatePass.m_PassIdentifierHandle) ?? new PassIdentifier();

        public IEnumerable<TemplatePassStageElement> VertexStageElements => GetStageElements(templatePass.m_VertexStageElementListHandle);
        public IEnumerable<TemplatePassStageElement> FragmentStageElements => GetStageElements(templatePass.m_FragmentStageElementListHandle);

        IEnumerable<TemplatePassStageElement> GetStageElements(FoundryHandle stageElementListHandle)
        {
            var localContainer = Container;
            var list = new FixedHandleListInternal(stageElementListHandle);
            return list.Select(localContainer, (handle) => (new TemplatePassStageElement(localContainer, handle)));
        }

        public IEnumerable<CommandDescriptor> CommandDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_CommandDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new CommandDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<PragmaDescriptor> PragmaDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_PragmaDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new PragmaDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<TagDescriptor> TagDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_TagDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new TagDescriptor(localContainer, handle)));
            }
        }

        public bool EnableDebugging => templatePass.m_EnableDebugging;

        // private
        internal TemplatePass(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.templatePass = container?.GetTemplatePass(handle) ?? TemplatePassInternal.Invalid();
        }

        public static TemplatePass Invalid => new TemplatePass(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is TemplatePass other && this.Equals(other);
        public bool Equals(TemplatePass other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TemplatePass lhs, TemplatePass rhs) => lhs.Equals(rhs);
        public static bool operator!=(TemplatePass lhs, TemplatePass rhs) => !lhs.Equals(rhs);

        internal class UsePassBuilder
        {
            ShaderContainer container;
            string usePassName;

            public UsePassBuilder(ShaderContainer container, string usePassName)
            {
                this.container = container;
                this.usePassName = usePassName;
            }

            public TemplatePass Build()
            {
                FoundryHandle invalid = FoundryHandle.Invalid();

                var templatePassInternal = new TemplatePassInternal()
                {
                    m_UsePassNameHandle = container.AddString(usePassName),
                    m_DisplayNameHandle = invalid,
                    m_ReferenceNameHandle = invalid,
                    m_PassIdentifierHandle = invalid,
                    m_VertexStageElementListHandle = invalid,
                    m_FragmentStageElementListHandle = invalid,
                    m_TagDescriptorListHandle = invalid,
                    m_EnableDebugging = false
                };

                var passHandle = container.AddTemplatePassInternal(templatePassInternal);
                return new TemplatePass(container, passHandle);
            }
        }

        public class Builder
        {
            internal class StageElement
            {
                internal BlockInstance BlockInstance = BlockInstance.Invalid;
                internal CustomizationPoint CustomizationPoint = CustomizationPoint.Invalid;
            }

            ShaderContainer container;
            PassIdentifierInternal passIdentifier = new PassIdentifierInternal(uint.MaxValue, uint.MaxValue);
            List<StageElement> vertexStageElements = new List<StageElement>();
            List<StageElement> fragmentStageElements = new List<StageElement>();
            List<CommandDescriptor> commandDescriptors;
            List<PragmaDescriptor> pragmaDescriptors;
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();

            public string DisplayName { get; set; }
            public string ReferenceName { get; set; }
            public bool EnableDebugging { get; set; }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
            }

            public void SetPassIdentifier(uint subShaderIndex, uint passIndex)
            {
                passIdentifier = new PassIdentifierInternal(subShaderIndex, passIndex);
            }

            public void AddCommandDescriptor(CommandDescriptor commandDescriptor)
            {
                if (commandDescriptors == null)
                    commandDescriptors = new List<CommandDescriptor>();
                commandDescriptors.Add(commandDescriptor);
            }

            public void AddPragmaDescriptor(PragmaDescriptor pragmaDescriptor)
            {
                if (pragmaDescriptors == null)
                    pragmaDescriptors = new List<PragmaDescriptor>();
                pragmaDescriptors.Add(pragmaDescriptor);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                tagDescriptors.Add(tagDescriptor);
            }

            public void AppendBlockInstance(BlockInstance blockInstance, Rendering.ShaderType stageType)
            {
                List<StageElement> stageElements = GetElementsForStage(stageType);
                if (stageElements == null)
                    throw new Exception($"Stage {stageType} is not valid.");

                stageElements.Add(new StageElement { BlockInstance = blockInstance });
            }

            public void AppendCustomizationPoint(CustomizationPoint customizationPoint, Rendering.ShaderType stageType)
            {
                List<StageElement> stageElements = GetElementsForStage(stageType);
                if (stageElements == null)
                    throw new Exception($"Stage {stageType} is not valid.");

                // Make sure the customization point hasn't been added multiple times.
                var element = stageElements.Find((e) => (e.CustomizationPoint == customizationPoint));
                if (element != null)
                    throw new Exception($"Customization point {customizationPoint.Name} cannot be added to a stage multiple times.");

                stageElements.Add(new StageElement { CustomizationPoint = customizationPoint });
            }

            List<StageElement> GetElementsForStage(Rendering.ShaderType stageType)
            {
                if (stageType == Rendering.ShaderType.Vertex)
                    return vertexStageElements;
                else if (stageType == Rendering.ShaderType.Fragment)
                    return fragmentStageElements;
                return null;
            }

            public TemplatePass Build()
            {
                var templatePassInternal = new TemplatePassInternal()
                {
                    m_UsePassNameHandle = FoundryHandle.Invalid(),
                    m_DisplayNameHandle = container.AddString(DisplayName),
                    m_ReferenceNameHandle = container.AddString(ReferenceName),
                    m_EnableDebugging = EnableDebugging,
                };

                templatePassInternal.m_PassIdentifierHandle = container.AddPassIdentifier(passIdentifier.SubShaderIndex, passIdentifier.PassIndex);
                templatePassInternal.m_VertexStageElementListHandle = FixedHandleListInternal.Build(container, vertexStageElements, (e) => BuildStageElement(e));
                templatePassInternal.m_FragmentStageElementListHandle = FixedHandleListInternal.Build(container, fragmentStageElements, (e) => BuildStageElement(e));
                templatePassInternal.m_CommandDescriptorListHandle = FixedHandleListInternal.Build(container, commandDescriptors, (o) => (o.handle));
                templatePassInternal.m_PragmaDescriptorListHandle = FixedHandleListInternal.Build(container, pragmaDescriptors, (o) => (o.handle));
                templatePassInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (o) => (o.handle));

                var returnTypeHandle = container.AddTemplatePassInternal(templatePassInternal);
                return new TemplatePass(container, returnTypeHandle);
            }

            FoundryHandle BuildStageElement(StageElement stageElement)
            {
                var blockInstanceHandle = stageElement.BlockInstance;
                var customizationPointHandle = stageElement.CustomizationPoint;
                var stageElementInternal = new TemplatePassStageElementInternal()
                {
                    m_BlockInstanceHandle = blockInstanceHandle.handle,
                    m_CustomizationPointHandle = customizationPointHandle.handle,
                };
                return container.AddTemplatePassStageElementInternal(stageElementInternal);
            }
        }
    }
}
