using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizManagement.ViewModels.Subjects;
using Services;
using System.Security.Claims;

namespace QuizManagement.Controllers
{
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        public IActionResult Index()
        {
            var subjects = _subjectService.GetSubjectsByUserId(CurrentUserId());
            return View(subjects);
        }

        public IActionResult Create()
        {
            return View(new SubjectFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(SubjectFormViewModel model)
        {
            model.Name = model.Name.Trim();

            if (_subjectService.NameExists(CurrentUserId(), model.Name))
            {
                ModelState.AddModelError(nameof(model.Name), "Tên môn học đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _subjectService.AddSubject(new Subject
            {
                UserId = CurrentUserId(),
                Name = model.Name,
                CreatedBy = User.Identity?.Name
            });

            TempData["SuccessMessage"] = "Đã tạo môn học.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id)
        {
            var subject = _subjectService.GetSubjectById(id, CurrentUserId());
            if (subject is null)
            {
                return NotFound();
            }

            return View(new SubjectFormViewModel
            {
                Id = subject.Id,
                Name = subject.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, SubjectFormViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            model.Name = model.Name.Trim();

            var subject = _subjectService.GetSubjectById(id, CurrentUserId());
            if (subject is null)
            {
                return NotFound();
            }

            if (_subjectService.NameExists(CurrentUserId(), model.Name, id))
            {
                ModelState.AddModelError(nameof(model.Name), "Tên môn học đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            subject.Name = model.Name;
            subject.UpdatedBy = User.Identity?.Name;
            _subjectService.UpdateSubject(subject);

            TempData["SuccessMessage"] = "Đã cập nhật môn học.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var subject = _subjectService.GetSubjectById(id, CurrentUserId());
            if (subject is null)
            {
                return NotFound();
            }

            subject.UpdatedBy = User.Identity?.Name;
            _subjectService.DeleteSubject(subject);

            TempData["SuccessMessage"] = "Đã xóa môn học.";
            return RedirectToAction(nameof(Index));
        }

        private string CurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Không tìm thấy UserId trong phiên đăng nhập.");
        }
    }
}
