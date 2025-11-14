using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanLyPhongTro.Models.ViewModels
{
    public class IndexViewModel
    {
        public IPagedList<Phong_Tro> PaginatedPosts { get; set; }
        public List<Phong_Tro> NewestPosts { get; set; }
    }
}