update {0} set Value = @Value 
	where Name = @Name; 
if @@ROWCOUNT = 0 
	insert into {0} (Name, Value) 
	values (@Name, @Value);