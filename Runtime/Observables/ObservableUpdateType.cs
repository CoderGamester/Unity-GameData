namespace GameLovers.GameData
{
	/// <summary>
	/// What happened to an observable collection's entry.
	/// </summary>
	public enum ObservableUpdateType
	{
		Added,
		Updated,
		Removed
	}

	/// <summary>
	/// Which subscribers an observable dictionary notifies on a change.
	/// </summary>
	public enum ObservableUpdateFlag
	{
		// Updates all subsribers that didn't specify the key index
		UpdateOnly,
		// Updates only for subscripers that added their key index
		KeyUpdateOnly,
		// Updates all types of subscribers [This has a high performance cost]
		Both
	}
}
