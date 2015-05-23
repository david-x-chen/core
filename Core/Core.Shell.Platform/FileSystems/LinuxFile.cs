using System;
using Core.Shell.Common.FileSystems;
using Core.IO;

namespace Core.Shell.Platform.FileSystems
{
	public class LinuxFile : RegularFile
	{
		public LinuxFile (string prefix, string path, RegularFileSystem fileSystem)
			: base (prefix, path, fileSystem)
		{
		}

		public override string PermissionsString { get { return FileHelper.Instance.PermissionsString (path: RealPath); } }

		public override string Owner { get { return Core.IO.FileHelper.Instance.GetOwner (path: RealPath); } }
	}

	public class LinuxDirectory : RegularDirectory
	{
		public LinuxDirectory (string prefix, string path, RegularFileSystem fileSystem)
			: base (prefix, path, fileSystem)
		{
		}

		public override string PermissionsString { get { return FileHelper.Instance.PermissionsString (path: RealPath); } }

		public override string Owner { get { return Core.IO.FileHelper.Instance.GetOwner (path: RealPath); } }
	}

	public class LinuxLink : RegularLink
	{
		public LinuxLink (string prefix, string path, RegularFileSystem fileSystem)
			: base (prefix, path, fileSystem)
		{
		}

		public override string PermissionsString { get { return FileHelper.Instance.PermissionsString (path: RealPath); } }

		public override string Owner { get { return Core.IO.FileHelper.Instance.GetOwner (path: RealPath); } }
	}
}

