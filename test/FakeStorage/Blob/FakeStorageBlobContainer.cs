﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FakeStorage
{
    internal class FakeStorageBlobContainer : CloudBlobContainer
    {
        internal readonly MemoryBlobStore _store;
        private readonly FakeStorageBlobClient _parent;

        public FakeStorageBlobContainer(FakeStorageBlobClient client, string containerName)
             : base(client.GetContainerUri(containerName))
        {
            if (string.IsNullOrWhiteSpace(this.Name))
            {
                throw new ArgumentException(nameof(containerName));
            }
            _store = client._store;
            _parent = client;
            
        }

        public override bool Equals(object obj)
        {
            if (obj is FakeStorageBlobContainer other)
            {
                return this.Uri == other.Uri;
            }
            return false;
        }

        internal Uri GetBlobUri(string blobName)
        {
            return new Uri(this.Uri.ToString() + "/" + blobName);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override CloudPageBlob GetPageBlobReference(string blobName)
        {
            return new FakeStoragePageBlob(blobName, this);
        }

        public override CloudPageBlob GetPageBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            return new FakeStoragePageBlob(blobName, this);
        }

        #region GetBlockBlobReference
        public override CloudBlockBlob GetBlockBlobReference(string blobName)
        {
            return base.GetBlockBlobReference(blobName);
        }

        public override CloudBlockBlob GetBlockBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            try
            {
                var blob = _store.GetBlobReferenceFromServer(this, this.Name, blobName);
                if (blob is CloudBlockBlob b)
                {
                    return b;
                }
            }
            catch { }
            return new FakeStorageBlockBlob(blobName, this);
        }
        #endregion

        public override CloudAppendBlob GetAppendBlobReference(string blobName)
        {
            return new FakeStorageAppendBlob(blobName, this);
        }

        public override CloudAppendBlob GetAppendBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            return new FakeStorageAppendBlob(blobName, this);
        }

        public override CloudBlob GetBlobReference(string blobName)
        {
            throw new NotImplementedException();
            return base.GetBlobReference(blobName);
        }

        public override CloudBlob GetBlobReference(string blobName, DateTimeOffset? snapshotTime)
        {
            throw new NotImplementedException();
            return base.GetBlobReference(blobName, snapshotTime);
        }

        // $$$ will built-in just work? 
        public override CloudBlobDirectory GetDirectoryReference(string relativeAddress)
        {
            // TestExtensions.NewCloudBlobDirectory()
            return base.GetDirectoryReference(relativeAddress);
        }

        public override Task CreateAsync()
        {
            throw new NotImplementedException();
            return base.CreateAsync();
        }

        public override Task CreateAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.CreateAsync(options, operationContext);
        }

        public override Task CreateAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.CreateAsync(accessType, options, operationContext);
        }

        public override Task CreateAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.CreateAsync(accessType, options, operationContext, cancellationToken);
        }

        public override Task<bool> CreateIfNotExistsAsync()
        {
            return base.CreateIfNotExistsAsync();
        }

        public override Task<bool> CreateIfNotExistsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return base.CreateIfNotExistsAsync(options, operationContext);
        }

        public override Task<bool> CreateIfNotExistsAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext)
        {
            return base.CreateIfNotExistsAsync(accessType, options, operationContext);
        }

        public override Task<bool> CreateIfNotExistsAsync(BlobContainerPublicAccessType accessType, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            _store.CreateIfNotExists(this.Name);
            return Task.FromResult(true); // $$$ what is this
        }

        public override Task DeleteAsync()
        {
            throw new NotImplementedException();
            return base.DeleteAsync();
        }

        public override Task DeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.DeleteAsync(accessCondition, options, operationContext);
        }

        public override Task DeleteAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.DeleteAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<bool> DeleteIfExistsAsync()
        {
            throw new NotImplementedException();
            return base.DeleteIfExistsAsync();
        }

        public override Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.DeleteIfExistsAsync(accessCondition, options, operationContext);
        }

        public override Task<bool> DeleteIfExistsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.DeleteIfExistsAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName)
        {
            throw new NotImplementedException();
            return base.GetBlobReferenceFromServerAsync(blobName);
        }

        public override Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.GetBlobReferenceFromServerAsync(blobName, accessCondition, options, operationContext);
        }

        public override Task<ICloudBlob> GetBlobReferenceFromServerAsync(string blobName, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            var blob = _store.GetBlobReferenceFromServer(this, this.Name, blobName);
            return Task.FromResult<ICloudBlob>(blob);
        }

        public override Task<BlobResultSegment> ListBlobsSegmentedAsync(BlobContinuationToken currentToken)
        {
            throw new NotImplementedException();
            return base.ListBlobsSegmentedAsync(currentToken);
        }

        public override Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, BlobContinuationToken currentToken)
        {
            throw new NotImplementedException();
            return base.ListBlobsSegmentedAsync(prefix, currentToken);
        }

        public override Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext);
        }

        public override Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails, int? maxResults, BlobContinuationToken currentToken, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext, cancellationToken);
        }

        public override Task SetPermissionsAsync(BlobContainerPermissions permissions)
        {
            throw new NotImplementedException();
            return base.SetPermissionsAsync(permissions);
        }

        public override Task SetPermissionsAsync(BlobContainerPermissions permissions, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.SetPermissionsAsync(permissions, accessCondition, options, operationContext);
        }

        public override Task SetPermissionsAsync(BlobContainerPermissions permissions, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.SetPermissionsAsync(permissions, accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<BlobContainerPermissions> GetPermissionsAsync()
        {
            throw new NotImplementedException();
            return base.GetPermissionsAsync();
        }

        public override Task<BlobContainerPermissions> GetPermissionsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.GetPermissionsAsync(accessCondition, options, operationContext);
        }

        public override Task<BlobContainerPermissions> GetPermissionsAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.GetPermissionsAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<bool> ExistsAsync()
        {
            return base.ExistsAsync();
        }

        public override Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext)
        {
            return base.ExistsAsync(options, operationContext);
        }

        public override Task<bool> ExistsAsync(BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(_store.Exists(this.Name));
        }

        public override Task FetchAttributesAsync()
        {
            throw new NotImplementedException();
            return base.FetchAttributesAsync();
        }

        public override Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.FetchAttributesAsync(accessCondition, options, operationContext);
        }

        public override Task FetchAttributesAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.FetchAttributesAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task SetMetadataAsync()
        {
            throw new NotImplementedException();
            return base.SetMetadataAsync();
        }

        public override Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.SetMetadataAsync(accessCondition, options, operationContext);
        }

        public override Task SetMetadataAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.SetMetadataAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            throw new NotImplementedException();
            return base.AcquireLeaseAsync(leaseTime, proposedLeaseId);
        }

        public override Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.AcquireLeaseAsync(leaseTime, proposedLeaseId, accessCondition, options, operationContext);
        }

        public override Task<string> AcquireLeaseAsync(TimeSpan? leaseTime, string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.AcquireLeaseAsync(leaseTime, proposedLeaseId, accessCondition, options, operationContext, cancellationToken);
        }

        public override Task RenewLeaseAsync(AccessCondition accessCondition)
        {
            throw new NotImplementedException();
            return base.RenewLeaseAsync(accessCondition);
        }

        public override Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.RenewLeaseAsync(accessCondition, options, operationContext);
        }

        public override Task RenewLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.RenewLeaseAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition)
        {
            throw new NotImplementedException();
            return base.ChangeLeaseAsync(proposedLeaseId, accessCondition);
        }

        public override Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.ChangeLeaseAsync(proposedLeaseId, accessCondition, options, operationContext);
        }

        public override Task<string> ChangeLeaseAsync(string proposedLeaseId, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.ChangeLeaseAsync(proposedLeaseId, accessCondition, options, operationContext, cancellationToken);
        }

        public override Task ReleaseLeaseAsync(AccessCondition accessCondition)
        {
            throw new NotImplementedException();
            return base.ReleaseLeaseAsync(accessCondition);
        }

        public override Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.ReleaseLeaseAsync(accessCondition, options, operationContext);
        }

        public override Task ReleaseLeaseAsync(AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.ReleaseLeaseAsync(accessCondition, options, operationContext, cancellationToken);
        }

        public override Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod)
        {
            throw new NotImplementedException();
            return base.BreakLeaseAsync(breakPeriod);
        }

        public override Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext)
        {
            throw new NotImplementedException();
            return base.BreakLeaseAsync(breakPeriod, accessCondition, options, operationContext);
        }

        public override Task<TimeSpan> BreakLeaseAsync(TimeSpan? breakPeriod, AccessCondition accessCondition, BlobRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            return base.BreakLeaseAsync(breakPeriod, accessCondition, options, operationContext, cancellationToken);
        }
    }
}
