﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NursingBot.Core;

#nullable disable

namespace NursingBot.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("NursingBot.Models.PartyChannel", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("ServerId");

                    b.ToTable("partyChannels");
                });

            modelBuilder.Entity("NursingBot.Models.PartyRecruit", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("AuthorId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime?>("ClosedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Date")
                        .HasColumnType("longtext");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsClosed")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("PartyChannelId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("PartyChannelId");

                    b.ToTable("partyRecruits");
                });

            modelBuilder.Entity("NursingBot.Models.Server", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<ulong>("DiscordUID")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("DiscordUID");

                    b.ToTable("servers");
                });

            modelBuilder.Entity("NursingBot.Models.Vote", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("AuthorId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Choices")
                        .HasColumnType("longtext");

                    b.Property<DateTime?>("ClosedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasColumnType("longtext");

                    b.Property<bool>("IsClosed")
                        .HasColumnType("tinyint(1)");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("bigint unsigned");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("ServerId");

                    b.ToTable("votes");
                });

            modelBuilder.Entity("NursingBot.Models.PartyChannel", b =>
                {
                    b.HasOne("NursingBot.Models.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Server");
                });

            modelBuilder.Entity("NursingBot.Models.PartyRecruit", b =>
                {
                    b.HasOne("NursingBot.Models.PartyChannel", "PartyChannel")
                        .WithMany()
                        .HasForeignKey("PartyChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("PartyChannel");
                });

            modelBuilder.Entity("NursingBot.Models.Vote", b =>
                {
                    b.HasOne("NursingBot.Models.Server", "Server")
                        .WithMany()
                        .HasForeignKey("ServerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Server");
                });
#pragma warning restore 612, 618
        }
    }
}
