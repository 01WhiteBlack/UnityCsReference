// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmCache : ISerializationCallbackReceiver
    {
        private Dictionary<string, PackageInfo> m_SearchPackageInfos = new Dictionary<string, PackageInfo>();
        private Dictionary<string, PackageInfo> m_InstalledPackageInfos = new Dictionary<string, PackageInfo>();
        private Dictionary<string, PackageInfo> m_ProductSearchPackageInfos = new Dictionary<string, PackageInfo>();
        private Dictionary<string, string> m_ProductIdToNameMap = new Dictionary<string, string>();
        private Dictionary<string, string> m_NameToProductIdMap = new Dictionary<string, string>();

        private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfo = new Dictionary<string, Dictionary<string, PackageInfo>>();

        private readonly Dictionary<string, Dictionary<string, object>> m_ParsedUpmReserved = new Dictionary<string, Dictionary<string, object>>();

        [SerializeField]
        private long m_SearchPackageInfosTimestamp = 0;

        // arrays created to help serialize dictionaries
        [SerializeField]
        private PackageInfo[] m_SerializedInstalledPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedSearchPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedProductSearchPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedExtraPackageInfos;

        public virtual event Action<IEnumerable<PackageInfo>> onPackageInfosUpdated;
        public virtual event Action<PackageInfo> onExtraPackageInfoFetched;
        public virtual event Action<string> onVerifiedGitPackageUpToDate;

        public virtual IEnumerable<PackageInfo> searchPackageInfos => m_SearchPackageInfos.Values;
        public virtual IEnumerable<PackageInfo> installedPackageInfos => m_InstalledPackageInfos.Values;
        public virtual IEnumerable<PackageInfo> productSearchPackageInfos => m_ProductSearchPackageInfos.Values;

        private static List<PackageInfo> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
        {
            return newInfos.Values.Where(p => !oldInfos.TryGetValue(p.name, out var info) || IsDifferent(info, p))
                .Concat(oldInfos.Values.Where(p => !newInfos.ContainsKey(p.name))).ToList();
        }

        // For BuiltIn and Registry packages, we want to only compare a subset of PackageInfo attributes,
        // as most attributes never change if their PackageId is the same. For other types of packages, always consider them different
        private static bool IsDifferent(PackageInfo p1, PackageInfo p2)
        {
            if (p1.packageId != p2.packageId ||
                p1.isDirectDependency != p2.isDirectDependency ||
                p1.version != p2.version ||
                p1.source != p2.source ||
                p1.resolvedPath != p2.resolvedPath ||
                p1.entitlements.isAllowed != p2.entitlements.isAllowed ||
                p1.entitlements.licensingModel != p2.entitlements.licensingModel ||
                p1.registry?.id != p2.registry?.id ||
                p1.registry?.name != p2.registry?.name ||
                p1.registry?.url != p2.registry?.url ||
                p1.registry?.isDefault != p2.registry?.isDefault ||
                p1.versions.recommended != p2.versions.recommended ||
                p1.versions.compatible.Length != p2.versions.compatible.Length || !p1.versions.compatible.SequenceEqual(p2.versions.compatible) ||
                p1.versions.all.Length != p2.versions.all.Length || !p1.versions.all.SequenceEqual(p2.versions.all) ||
                p1.errors.Length != p2.errors.Length || !p1.errors.SequenceEqual(p2.errors) ||
                p1.dependencies.Length != p2.dependencies.Length || !p1.dependencies.SequenceEqual(p2.dependencies) ||
                p1.resolvedDependencies.Length != p2.resolvedDependencies.Length || !p1.resolvedDependencies.SequenceEqual(p2.resolvedDependencies) ||
                p1.projectDependenciesEntry != p2.projectDependenciesEntry ||
                p1.signature.status != p2.signature.status ||
                p1.documentationUrl != p2.documentationUrl ||
                p1.changelogUrl != p2.changelogUrl ||
                p1.licensesUrl != p2.licensesUrl ||
                p1.assetStore?.productId != p2.assetStore?.productId)
                return true;

            if (p1.source == PackageSource.BuiltIn || p1.source == PackageSource.Registry)
                return false;

            if (p1.source == PackageSource.Git)
                return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

            return true;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedInstalledPackageInfos = m_InstalledPackageInfos.Values.ToArray();
            m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
            m_SerializedProductSearchPackageInfos = m_ProductSearchPackageInfos.Values.ToArray();
            m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedInstalledPackageInfos)
            {
                m_InstalledPackageInfos[p.name] = p;
                UpdateProductIdAndNameMapping(p);
            }

            m_SearchPackageInfos = m_SerializedSearchPackageInfos.ToDictionary(p => p.name, p => p);

            foreach (var p in m_SerializedProductSearchPackageInfos)
            {
                m_ProductSearchPackageInfos[p.name] = p;
                UpdateProductIdAndNameMapping(p);
            }

            foreach (var p in m_SerializedExtraPackageInfos)
                AddExtraPackageInfo(p);
        }

        private void UpdateProductIdAndNameMapping(PackageInfo info)
        {
            var productId = info.assetStore?.productId;
            if (!string.IsNullOrEmpty(productId))
            {
                m_ProductIdToNameMap[productId] = info.name;
                m_NameToProductIdMap[info.name] = productId;
            }
        }

        public virtual void AddExtraPackageInfo(PackageInfo packageInfo)
        {
            if (!m_ExtraPackageInfo.TryGetValue(packageInfo.name, out var dict))
            {
                dict = new Dictionary<string, PackageInfo>();
                m_ExtraPackageInfo[packageInfo.name] = dict;
            }
            dict[packageInfo.version] = packageInfo;
            onExtraPackageInfoFetched?.Invoke(packageInfo);
        }

        public virtual Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName) => m_ExtraPackageInfo.Get(packageName);

        public virtual void RemoveInstalledPackageInfo(string packageName)
        {
            var oldInfo = m_InstalledPackageInfos.Get(packageName);
            if (oldInfo == null)
                return;

            m_InstalledPackageInfos.Remove(packageName);
            TriggerOnPackageInfosUpdated(new PackageInfo[] { oldInfo });
        }

        public virtual bool IsPackageInstalled(string packageName) => m_InstalledPackageInfos.ContainsKey(packageName);

        public virtual PackageInfo GetInstalledPackageInfo(string packageName) => m_InstalledPackageInfos.Get(packageName);

        public virtual PackageInfo GetInstalledPackageInfoById(string packageId)
        {
            var idSplit = packageId?.Split(new[] { '@' }, 2);
            return idSplit?.Length == 2 ? GetInstalledPackageInfo(idSplit[0]) : null;
        }

        public virtual void SetInstalledPackageInfo(PackageInfo info, bool isSpecialInstallation)
        {
            var oldInfo = m_InstalledPackageInfos.Get(info.name);
            m_InstalledPackageInfos[info.name] = info;
            UpdateProductIdAndNameMapping(info);
            if (isSpecialInstallation || oldInfo == null || IsDifferent(oldInfo, info))
                TriggerOnPackageInfosUpdated(new PackageInfo[] { info });

            // if Git install and oldInfo is same as new info, means no update was found
            else if (oldInfo.source == PackageSource.Git && !IsDifferent(oldInfo, info))
            {
                onVerifiedGitPackageUpToDate.Invoke(oldInfo.name);
            }
        }

        public virtual void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_InstalledPackageInfos;
            m_InstalledPackageInfos = newPackageInfos;
            foreach (var info in installedPackageInfos)
                UpdateProductIdAndNameMapping(info);

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);

            // This is to fix the issue where refresh in `In Project` doesn't show new versions from the registry
            // The cause of that issue is that when we create a UpmPackage, we take the versions from searchInfo
            // and augment with the installed version but the versions in searchInfo could be outdated sometimes
            // Since we have no easy way to compare two package info and know which one is newer with the current implementation,
            // we want to keep what's stored in the searchPackageInfos as up to date as possible,
            if (timestamp > m_SearchPackageInfosTimestamp)
                foreach (var newInfo in updatedInfos.Where(info => m_SearchPackageInfos.ContainsKey(info.name) && info.errors.Length == 0))
                    m_SearchPackageInfos[newInfo.name] = newInfo;

            if (updatedInfos.Any())
                TriggerOnPackageInfosUpdated(updatedInfos);
        }

        public virtual PackageInfo GetSearchPackageInfo(string packageName) => m_SearchPackageInfos.Get(packageName);

        public virtual void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_SearchPackageInfos;
            m_SearchPackageInfos = newPackageInfos;

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);
            if (updatedInfos.Any())
                TriggerOnPackageInfosUpdated(updatedInfos);

            m_SearchPackageInfosTimestamp = timestamp;
        }

        public virtual PackageInfo GetProductSearchPackageInfo(string packageName) => m_ProductSearchPackageInfos.Get(packageName);
        public virtual void SetProductSearchPackageInfo(string productId, PackageInfo info)
        {
            var oldInfo = m_ProductSearchPackageInfos.Get(info.name);
            m_ProductSearchPackageInfos[info.name] = info;
            UpdateProductIdAndNameMapping(info);
            if (oldInfo == null || IsDifferent(oldInfo, info))
                TriggerOnPackageInfosUpdated(new PackageInfo[] { info });
        }

        private void TriggerOnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
        {
            foreach (var info in packageInfos)
                m_ParsedUpmReserved.Remove(info.packageId);
            onPackageInfosUpdated?.Invoke(packageInfos);
        }

        public virtual Dictionary<string, object> ParseUpmReserved(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.upmReserved))
                return null;

            if (!m_ParsedUpmReserved.TryGetValue(packageInfo.packageId, out var result))
            {
                result = Json.Deserialize(packageInfo.upmReserved) as Dictionary<string, object>;
                m_ParsedUpmReserved[packageInfo.packageId] = result;
            }
            return result;
        }

        public virtual string GetProductIdByName(string packageName) => m_NameToProductIdMap.Get(packageName);
        public virtual string GetNameByProductId(string productId) => m_ProductIdToNameMap.Get(productId);

        public virtual void ClearCache()
        {
            m_InstalledPackageInfos.Clear();
            m_SearchPackageInfos.Clear();
            m_ExtraPackageInfo.Clear();

            m_SerializedInstalledPackageInfos = new PackageInfo[0];
            m_SerializedSearchPackageInfos = new PackageInfo[0];
            m_SerializedExtraPackageInfos = new PackageInfo[0];

            ClearProductCache();
        }

        public virtual void ClearProductCache()
        {
            m_ProductSearchPackageInfos.Clear();
            m_SerializedProductSearchPackageInfos = new PackageInfo[0];
            m_ProductIdToNameMap.Clear();
            m_NameToProductIdMap.Clear();
            foreach (var info in installedPackageInfos)
                UpdateProductIdAndNameMapping(info);
        }
    }
}
