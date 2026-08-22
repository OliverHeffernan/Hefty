namespace Hefty.Engine;

public interface IDestroyable
{
	void CleanUp() { }
	void Destroy() { CleanUp(); }
	bool ToDestroy { get => false; }
}
