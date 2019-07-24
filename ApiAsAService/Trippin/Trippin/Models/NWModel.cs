// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace ODataDemo
{
    public class NWModel : DbContext
    {
        static NWModel()
        {
            Database.SetInitializer(new NwDatabaseInitializer());
        }

        public NWModel()
            : base("name=NWModel")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Person> Persons { get; set; }

        private static NWModel instance;
        public static NWModel Instance
        {
            get
            {
                if (instance == null)
                {
                    ResetDataSource();
                }
                return instance;
            }
        }

        public static void ResetDataSource()
        {
            instance = new NWModel();

            #region Products

            var products = new List<Product>
            {
                new Product
                {
                    ProductID=1,
                    Name = "widget",
                },

                new Product
                {
                    ProductID=2,
                    Name = "Gadget",
                },
            };

            instance.Products.AddRange(products);

            #endregion


            instance.Persons.AddRange(new List<Person>
            {
                new Person
                {
                    ID = 1,
                    Name = "Willie",
                },
                new Person
                {
                    ID = 2,
                    Name = "Vincent",
                },
            });

            instance.SaveChanges();
        }
    }

    class NwDatabaseInitializer : DropCreateDatabaseAlways<NWModel>
    {
        protected override void Seed(NWModel context)
        {
            NWModel.ResetDataSource();
        }
    }

    public class Product
    {
        [Key]
        public Int32 ProductID;
        public string Name;
        public string Description;
        public DateTimeOffset ReleaseDate;
        public DateTimeOffset DiscontinuedDate;
        public Int16 Price;
        public virtual ICollection<Category> Categories { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual ProductDetail ProductDetail { get; set; }
    }

    public class ProductDetail
    {
        [Key]
        public Int32 ProductID;
        public string Details;
        public virtual Product Product { get; set; }
    }

    public class Category
    {
        [Key]
        public Int32 ID;
        public string Name;
        public virtual ICollection<Product> Products {get; set;}
    }

    public class Supplier
    {
        [Key]
        public Int32 Id;
        public string Name;
        public Address Address;
        public Int32 Concurrency;
        public virtual ICollection<Product> Products { get; set; }
    }

    public class Address
    {
        public string Street;
        public string City;
        public string State;
        public string ZipCode;
        public string Country;
    }

    public class Person
    {
        [Key]
        public Int32 ID;
        public string Name;
        public virtual PersonDetail PersonDetail { get; set; }
    }

    public class Customer : Person
    {
        public Decimal TotalExpense;
    }

    public class Employee : Person
    {
        [Key]
        public Int64 EmployeeId;
        public DateTimeOffset HireDate;
        public Single Salary;
    }

    public class PersonDetail
    {
        [Key]
        public Int32 PersonId;
        public int Age;
        public bool Gender;
        public string Phone;
        public Address Address;
        public virtual Person Person { get; set; }
    }
}
