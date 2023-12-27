declare r int; 
	begin 
	update configs 
		set value = @Value 
		where name = @Name; 
	get diagnostics r = row_count; 
	if r = 0 then 
		insert into configs (name, value) 
		values (@Name, @Value); 
	end if;