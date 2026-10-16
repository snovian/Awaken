# Awaken

仿 PowerToys Awake 的 Windows 保活小工具：托盘程序，阻止系统休眠/熄屏。
单文件绿色版、无需安装、无需 UAC（管理员权限）。

> 本项目与 Microsoft 无关，未获 Microsoft 认可。设计参考见
> [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)，代码采用 [MIT](LICENSE) 许可证。

## 功能

- **关闭** — 不干预，恢复正常电源策略
- **保持唤醒** — 系统不休眠，屏幕可熄灭
- **定时保持唤醒** — 30 分钟 / 1 小时 / 2 小时 / 自定义，到期自动恢复，托盘提示显示剩余时间
- **保持唤醒直至...**（一级菜单）— 到今天指定时刻为止（仅时间无日期，已过时刻不生效），激活后菜单显示如"保持唤醒直至 18:00"
- **屏幕常亮**（独立叠加开关）— 勾选后，无论"保持唤醒"还是"定时保持唤醒"，屏幕一律不熄
- **设置** — 勾选常用时长（30 分钟～12 小时），勾选项显示在一级菜单（如"保持唤醒 30 分钟"），一键开始定时
- **开机自启** — 写 HKCU Run 键，免管理员；exe 移动位置后下次启动自动修正路径
- 单实例保护；配置便携化（exe 旁 `config.json`，目录不可写时降级到 `%LocalAppData%\Awaken`）

## 原理

核心是 Win32 `SetThreadExecutionState` API（纯用户态）：

- `ES_CONTINUOUS | ES_SYSTEM_REQUIRED` — 保持系统唤醒
- `ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED` — 唤醒 + 屏幕常亮

`ES_CONTINUOUS` 绑定在调用线程上，因此所有状态切换由一个专用线程串行执行（UI 投递、引擎线程应用），退出前显式重置。

## 构建与发布

需要 .NET 10 SDK（`winget` 或 [dotnet-install 脚本](https://dotnet/v1/dotnet-install.ps1) 安装即可，SDK 用户级安装亦可）。

```powershell
# 开发构建
dotnet build src/Awaken -c Release

# 绿色版单文件发布（win-x64 / win-arm64）
./publish.ps1
```

产物为 `publish/win-x64/Awaken.exe`（约 15 MB），复制到任意目录双击即可运行，目标机不需要安装 .NET。

## 使用

1. 运行 `Awaken.exe`，托盘出现绿色圆点图标
2. 右键托盘图标选择模式：灰圈 = 未激活，绿圆 = 保持唤醒，蓝圆白条 = 屏幕常亮，绿圆白表盘 = 定时模式
3. 退出时自动恢复正常电源策略

## 布局

```
src/Awaken/
  Program.cs                    入口 + 单实例互斥
  Core/                         ExecutionState、P/Invoke、唤醒引擎（专用线程）
  UI/                           托盘菜单、程序化图标、定时时长对话框
  Config/                       便携配置存取（exe 旁 + AppData 降级）
  Integration/                  HKCU Run 开机自启
publish.ps1                     绿色版发布脚本
```
