﻿using Blog.Common;
using Blog.Data.EFEntities;
//using Blog.Model.Entity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace Blog.Data.Repository
{
    public class RepositoryBase<TEntity> : IRepositoryBase<TEntity> where TEntity : class, new()
    {
        //public ChinaEduDbContext dbcontext = new ChinaEduDbContext();
        private BlogDbContext dbcontext = new BlogDbContext();


        public int SubmitForm(TEntity entity, Expression<Func<TEntity, bool>> predicate = null)
        {
            int re = 0;
            if (predicate != null)
            {


            }
            return re;
        }



        public int Insert(TEntity entity)
        {
            dbcontext.Entry<TEntity>(entity).State = EntityState.Added;
            return dbcontext.SaveChanges();
        }
        public int Insert(List<TEntity> entitys)
        {
            foreach (var entity in entitys)
            {
                dbcontext.Entry<TEntity>(entity).State = EntityState.Added;
            }
            return dbcontext.SaveChanges();
        }
        public int Update(TEntity entity)
        {
            dbcontext.Set<TEntity>().Attach(entity);
            PropertyInfo[] props = entity.GetType().GetProperties();
            foreach (PropertyInfo prop in props)
            {
                if (ExcludeType.IsExclude(prop))
                {
                    if (prop.GetValue(entity, null) != null)
                    {
                        if (prop.GetValue(entity, null).ToString() == "&nbsp;")
                            dbcontext.Entry(entity).Property(prop.Name).CurrentValue = null;
                        dbcontext.Entry(entity).Property(prop.Name).IsModified = true;
                    }
                    else
                    {
                        dbcontext.Entry(entity).Property(prop.Name).IsModified = true;
                    }
                }
            }
            return dbcontext.SaveChanges();
        }


        /// <summary>
        /// sql更新
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public int Update(string sql, params SqlParameter[] param)
        {
            return dbcontext.Database.ExecuteSqlCommand(sql, param);
        }



        public int Update<PEntity>(PEntity entity)
        {
            var model = DTOHelper<PEntity, TEntity>.Trans(entity);
            return Update(model);


        }


        public int Delete(TEntity entity)
        {
            dbcontext.Set<TEntity>().Attach(entity);
            dbcontext.Entry<TEntity>(entity).State = EntityState.Deleted;
            return dbcontext.SaveChanges();
        }

        public int Delete(Expression<Func<TEntity, bool>> predicate)
        {
            var entitys = dbcontext.Set<TEntity>().Where(predicate).ToList();
            entitys.ForEach(m => dbcontext.Entry<TEntity>(m).State = EntityState.Deleted);
            return dbcontext.SaveChanges();
        }
        public TEntity FindEntity(object keyValue)
        {
            return dbcontext.Set<TEntity>().Find(keyValue);
        }
        public TEntity FindEntity(Expression<Func<TEntity, bool>> predicate)
        {
            return dbcontext.Set<TEntity>().FirstOrDefault(predicate);
        }


        //public REntity FindEntity<REntity>(object keyValue)
        //{
        //    var model = dbcontext.Set<TEntity>().Find(keyValue);

        //    return DTOHelper<TEntity, REntity>.Trans(model);
        //}
        //public REntity FindEntity<REntity>(Expression<Func<TEntity, bool>> predicate)
        //{
        //    var model = dbcontext.Set<TEntity>().FirstOrDefault(predicate);
        //    return DTOHelper<TEntity, REntity>.Trans(model);
        //}
        public IQueryable<TEntity> IQueryable()
        {
            return dbcontext.Set<TEntity>();
        }
        public IQueryable<TEntity> IQueryable(Expression<Func<TEntity, bool>> predicate)
        {
            return dbcontext.Set<TEntity>().Where(predicate);
        }
        public List<TEntity> FindList(string strSql)
        {
            return dbcontext.Database.SqlQuery<TEntity>(strSql).ToList<TEntity>();
        }
        public List<TEntity> FindList(string strSql, Pagination pagination)
        {

            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            var tempData = dbcontext.Database.SqlQuery<TEntity>(strSql).AsQueryable();
            pagination.records = tempData.Count();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }
        public List<TEntity> FindList(string strSql, List<SqlParameter> dbParameter, Pagination pagination)
        {
            var tempData = dbcontext.Database.SqlQuery<TEntity>(strSql, dbParameter.ToArray()).AsQueryable();
            pagination.records = tempData.Count();
            tempData = dbcontext.Database.SqlQuery<TEntity>(strSql, dbParameter.Select(t => ((ICloneable)t).Clone()).ToArray()).AsQueryable();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }
        public List<TEntity> FindList(string strSql, DbParameter[] dbParameter)
        {
            return dbcontext.Database.SqlQuery<TEntity>(strSql, dbParameter).ToList<TEntity>();
        }
        public List<TEntity> FindList(Pagination pagination)
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = dbcontext.Set<TEntity>().AsQueryable();
            foreach (string item in _order)
            {
                string _orderPart = item;
                _orderPart = Regex.Replace(_orderPart, @"\s+", " ");
                string[] _orderArry = _orderPart.Split(' ');
                string _orderField = _orderArry[0];
                bool sort = isAsc;
                if (_orderArry.Length == 2)
                {
                    isAsc = _orderArry[1].ToUpper() == "ASC" ? true : false;
                }
                var parameter = Expression.Parameter(typeof(TEntity), "t");
                var property = typeof(TEntity).GetProperty(_orderField);
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);
                resultExp = Expression.Call(typeof(Queryable), isAsc ? "OrderBy" : "OrderByDescending", new Type[] { typeof(TEntity), property.PropertyType }, tempData.Expression, Expression.Quote(orderByExp));
            }
            tempData = tempData.Provider.CreateQuery<TEntity>(resultExp);
            pagination.records = tempData.Count();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }
        public List<TEntity> FindList(Expression<Func<TEntity, bool>> predicate, Pagination pagination)
        {
            bool isAsc = pagination.sord.ToLower() == "asc" ? true : false;
            string[] _order = pagination.sidx.Split(',');
            MethodCallExpression resultExp = null;
            var tempData = dbcontext.Set<TEntity>().Where(predicate);
            foreach (string item in _order)
            {
                string _orderPart = item;
                _orderPart = Regex.Replace(_orderPart, @"\s+", " ");
                string[] _orderArry = _orderPart.Split(' ');
                string _orderField = _orderArry[0];
                bool sort = isAsc;
                if (_orderArry.Length == 2)
                {
                    isAsc = _orderArry[1].ToUpper() == "ASC" ? true : false;
                }
                var parameter = Expression.Parameter(typeof(TEntity), "t");
                var property = typeof(TEntity).GetProperty(_orderField);
                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var orderByExp = Expression.Lambda(propertyAccess, parameter);
                resultExp = Expression.Call(typeof(Queryable), isAsc ? "OrderBy" : "OrderByDescending", new Type[] { typeof(TEntity), property.PropertyType }, tempData.Expression, Expression.Quote(orderByExp));
            }
            tempData = tempData.Provider.CreateQuery<TEntity>(resultExp);
            pagination.records = tempData.Count();
            tempData = tempData.Skip<TEntity>(pagination.rows * (pagination.page - 1)).Take<TEntity>(pagination.rows).AsQueryable();
            return tempData.ToList();
        }

        public bool Exists(Expression<Func<TEntity, bool>> predicate)
        {
            return dbcontext.Set<TEntity>().AsNoTracking().Any(predicate);
        }

        public List<TEntity> FindList(string strSql, SqlParameter[] SqlParameter, Pagination pagination)
        {
            throw new NotImplementedException();
        }

        public List<TEntity> FindList(string strSql, SqlParameter[] SqlParameter)
        {
            throw new NotImplementedException();
        }

    }
}