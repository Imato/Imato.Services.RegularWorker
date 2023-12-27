update workers 
	set executed = getdate() 
	where id = @id; 
select executed from workers where id = @id