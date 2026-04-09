using Microsoft.EntityFrameworkCore;
using IWX_CloudZen.Authentication.Models.Entities;
using IWX_CloudZen.CloudAccounts.Entities;
using IWX_CloudZen.CloudServices.CloudStorage.Entities;
using IWX_CloudZen.CloudServices.Cluster.Entities;
using IWX_CloudZen.CloudServices.VPC.Entities;
using IWX_CloudZen.Permissions.Entities;
using IWX_CloudZen.CloudServices.ECR.Entities;
using IWX_CloudZen.CloudServices.ECS.Entities;
using IWX_CloudZen.CloudServices.Subnet.Entities;
using IWX_CloudZen.CloudServices.SecurityGroups.Entities;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Entities;
using IWX_CloudZen.CloudServices.EC2.Entities;
using IWX_CloudZen.CloudServices.KeyPair.Entities;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Entities;

namespace IWX_CloudZen.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CloudAccount> CloudAccounts { get; set; }
        public DbSet<CloudFile> CloudFiles { get; set; }
        public DbSet<BucketRecord> BucketRecords { get; set; }
        public DbSet<ClusterRecord> ClusterRecords { get; set; }
        public DbSet<VpcRecord> VpcRecords { get; set; }
        public DbSet<PolicyRecord> PolicyRecords { get; set; }
        public DbSet<EcrRepositoryRecord> EcrRepositoryRecords { get; set; }
        public DbSet<EcrImageRecord> EcrImageRecords { get; set; }
        public DbSet<EcsTaskDefinitionRecord> EcsTaskDefinitionRecords { get; set; }
        public DbSet<EcsServiceRecord> EcsServiceRecords { get; set; }
        public DbSet<EcsTaskRecord> EcsTaskRecords { get; set; }
        public DbSet<SubnetRecord> SubnetRecords { get; set; }
        public DbSet<SecurityGroupRecord> SecurityGroupRecords { get; set; }
        public DbSet<LogGroupRecord> LogGroupRecords { get; set; }
        public DbSet<LogStreamRecord> LogStreamRecords { get; set; }
        public DbSet<Ec2InstanceRecord> Ec2InstanceRecords { get; set; }
        public DbSet<KeyPairRecord> KeyPairRecords { get; set; }
        public DbSet<Ec2InstanceConnectEndpointRecord> Ec2InstanceConnectEndpointRecords { get; set; }
        public DbSet<Ec2InstanceConnectSessionRecord> Ec2InstanceConnectSessionRecords { get; set; }

        // 'DbSet<User> Users;' This is a table that stores User objects

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CloudAccount>()
                .HasIndex(x => new { x.UserEmail, x.Provider, x.AccountName })
                .IsUnique();
        }
    }
}
