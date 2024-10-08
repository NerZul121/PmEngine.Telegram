﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PmEngine.Telegram;

#nullable disable

namespace PmEngine.Telegram.Migrations
{
    [DbContext(typeof(TelegramContext))]
    [Migration("20241007152904_MessagesQueue")]
    partial class MessagesQueue
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PmEngine.Core.Entities.UserEntity", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("LastOnlineDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("RegistrationDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("SessionData")
                        .HasColumnType("text");

                    b.Property<int>("UserType")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("UserEntity");
                });

            modelBuilder.Entity("PmEngine.Telegram.Entities.MessageQueueEntity", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("numeric");

                    b.Property<string>("Actions")
                        .HasColumnType("text");

                    b.Property<string>("Arguments")
                        .HasColumnType("text");

                    b.Property<long>("ForUserId")
                        .HasColumnType("bigint");

                    b.Property<string>("Media")
                        .HasColumnType("text");

                    b.Property<int?>("MessageId")
                        .HasColumnType("integer");

                    b.Property<DateTime?>("SendedDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Text")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("MessageQueueEntity");
                });

            modelBuilder.Entity("PmEngine.Telegram.Entities.TelegramUserEntity", b =>
                {
                    b.Property<long>("TGID")
                        .HasColumnType("bigint");

                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<long>("OwnerId")
                        .HasColumnType("bigint");

                    b.HasKey("TGID", "ChatId");

                    b.HasIndex("OwnerId");

                    b.ToTable("TelegramUserEntity");
                });

            modelBuilder.Entity("PmEngine.Telegram.Entities.TelegramUserEntity", b =>
                {
                    b.HasOne("PmEngine.Core.Entities.UserEntity", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });
#pragma warning restore 612, 618
        }
    }
}
