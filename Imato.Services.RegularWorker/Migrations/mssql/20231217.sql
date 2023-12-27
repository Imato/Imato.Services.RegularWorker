alter table dbo.Workers 
	add executed datetime, hosts int;
go



create proc dbo.SetWorker
	@id int,
	@name varchar(255),
	@appName varchar(512),
	@active bit,
	@host varchar(255),
	@settings varchar(2000)
as 
begin 
	set nocount on;

	declare @hosts int = 0;

	begin transaction; 

	select @hosts = count(1)
		from dbo.Workers 
		where name = @name 
			and date >= dateadd(millisecond, 60000, getdate())

	update dbo.Workers 
		set date = getdate(), 
			active = @active,
			hosts = @hosts
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 
				
	if @@ROWCOUNT = 0 
		insert into dbo.Workers 
			(name, host, appName, date, settings, active, hosts) 
		values 
			(@name, @host, @appName, getdate(), @settings, @active, @hosts + 1); 
			
	select top 1 * 
		from dbo.Workers 
		where (@id > 0 and id = @id) 
			or (host = @host 
				and name = @name 
				and appName = @appName); 

	commit transaction;
end;
go